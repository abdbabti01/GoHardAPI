import 'dart:async';
import 'package:flutter/foundation.dart';
import 'package:isar/isar.dart';
import '../../data/services/api_service.dart';
import '../../data/local/services/local_database_service.dart';
import '../../data/local/models/local_session.dart';
import '../../core/constants/api_config.dart';
import 'connectivity_service.dart';

/// Service for automatic background synchronization of offline data
class SyncService {
  static SyncService? _instance;
  final ApiService _apiService;
  final LocalDatabaseService _localDb;
  final ConnectivityService _connectivity;

  Timer? _periodicSyncTimer;
  StreamSubscription<bool>? _connectivitySubscription;
  bool _isSyncing = false;
  bool _isInitialized = false;

  // Sync configuration
  static const Duration _syncInterval = Duration(minutes: 5);
  static const int _maxRetries = 3;
  static const Duration _syncDebounce = Duration(seconds: 3);

  Timer? _debounceTimer;

  /// Private constructor for singleton pattern
  SyncService._(this._apiService, this._localDb, this._connectivity);

  /// Factory constructor to create/get singleton instance
  factory SyncService({
    required ApiService apiService,
    required LocalDatabaseService localDb,
    required ConnectivityService connectivity,
  }) {
    _instance ??= SyncService._(apiService, localDb, connectivity);
    return _instance!;
  }

  /// Get singleton instance (must be initialized first)
  static SyncService get instance {
    if (_instance == null) {
      throw Exception('SyncService not initialized. Call factory constructor first.');
    }
    return _instance!;
  }

  /// Initialize sync service
  Future<void> initialize() async {
    if (_isInitialized) return;

    // Listen for connectivity changes
    _connectivitySubscription = _connectivity.connectivityStream.listen(
      (isOnline) {
        if (isOnline) {
          debugPrint('🔄 Network connected - scheduling sync');
          _scheduleDebouncedSync();
        } else {
          debugPrint('📴 Network disconnected - canceling sync');
          _cancelDebouncedSync();
        }
      },
    );

    // Start periodic sync timer (only syncs when online)
    _periodicSyncTimer = Timer.periodic(_syncInterval, (_) {
      if (_connectivity.isOnline && !_isSyncing) {
        debugPrint('⏰ Periodic sync triggered');
        sync();
      }
    });

    _isInitialized = true;
    debugPrint('✅ SyncService initialized');

    // Run initial sync if online
    if (_connectivity.isOnline) {
      _scheduleDebouncedSync();
    }
  }

  /// Schedule a debounced sync (prevents rapid sync attempts during network flapping)
  void _scheduleDebouncedSync() {
    _debounceTimer?.cancel();
    _debounceTimer = Timer(_syncDebounce, () {
      if (_connectivity.isOnline && !_isSyncing) {
        sync();
      }
    });
  }

  /// Cancel pending debounced sync
  void _cancelDebouncedSync() {
    _debounceTimer?.cancel();
  }

  /// Manually trigger sync (public API)
  Future<void> sync() async {
    if (_isSyncing) {
      debugPrint('⏭️ Sync already in progress, skipping');
      return;
    }

    if (!_connectivity.isOnline) {
      debugPrint('📴 Offline, skipping sync');
      return;
    }

    _isSyncing = true;
    debugPrint('🔄 Starting sync...');

    try {
      final db = _localDb.database;

      // Sync in order: Sessions → Exercises → Sets
      await _syncSessions(db);
      // TODO: Add exercise and set syncing in future phases

      debugPrint('✅ Sync completed successfully');
    } catch (e) {
      debugPrint('❌ Sync failed: $e');
    } finally {
      _isSyncing = false;
    }
  }

  /// Sync all pending sessions
  Future<void> _syncSessions(Isar db) async {
    final pendingSessions = await db.localSessions
        .filter()
        .isSyncedEqualTo(false)
        .findAll();

    if (pendingSessions.isEmpty) {
      debugPrint('  No pending sessions to sync');
      return;
    }

    debugPrint('  Syncing ${pendingSessions.length} sessions...');

    for (final session in pendingSessions) {
      try {
        switch (session.syncStatus) {
          case 'pending_create':
            await _syncCreateSession(db, session);
            break;
          case 'pending_update':
            await _syncUpdateSession(db, session);
            break;
          case 'pending_delete':
            await _syncDeleteSession(db, session);
            break;
          default:
            debugPrint('  Unknown sync status: ${session.syncStatus}');
        }
      } catch (e) {
        await _markSyncError(db, session, e.toString());
      }
    }
  }

  /// Sync a session that needs to be created on the server
  Future<void> _syncCreateSession(Isar db, LocalSession localSession) async {
    debugPrint('  Creating session ${localSession.localId} on server...');

    try {
      // POST to server
      final response = await _apiService.post<Map<String, dynamic>>(
        ApiConfig.sessions,
        data: {
          'userId': localSession.userId,
          'date': localSession.date.toIso8601String(),
          'duration': localSession.duration,
          'notes': localSession.notes,
          'type': localSession.type,
          'status': localSession.status,
          'startedAt': localSession.startedAt?.toIso8601String(),
          'completedAt': localSession.completedAt?.toIso8601String(),
          'pausedAt': localSession.pausedAt?.toIso8601String(),
        },
      );

      // Update local session with server ID
      await db.writeTxn(() async {
        localSession.serverId = response['id'] as int;
        localSession.isSynced = true;
        localSession.syncStatus = 'synced';
        localSession.lastModifiedServer = DateTime.parse(response['date'] as String);
        localSession.syncRetryCount = 0;
        localSession.syncError = null;
        localSession.lastSyncAttempt = DateTime.now();
        await db.localSessions.put(localSession);
      });

      debugPrint('  ✅ Session created with server ID: ${localSession.serverId}');
    } catch (e) {
      rethrow;
    }
  }

  /// Sync a session that needs to be updated on the server
  Future<void> _syncUpdateSession(Isar db, LocalSession localSession) async {
    if (localSession.serverId == null) {
      debugPrint('  ⚠️ Cannot update session without server ID');
      return;
    }

    debugPrint('  Updating session ${localSession.serverId} on server...');

    try {
      // Fetch current server version to check for conflicts
      final serverData = await _apiService.get<Map<String, dynamic>>(
        ApiConfig.sessionById(localSession.serverId!),
      );

      final serverModified = DateTime.parse(serverData['date'] as String);

      // Server-wins conflict resolution
      if (localSession.lastModifiedServer != null &&
          serverModified.isAfter(localSession.lastModifiedServer!)) {
        debugPrint('  ⚠️ Conflict detected - server has newer data, discarding local changes');

        // Update local with server data (server wins)
        await db.writeTxn(() async {
          localSession.status = serverData['status'] as String;
          localSession.notes = serverData['notes'] as String?;
          localSession.duration = serverData['duration'] as int?;
          localSession.type = serverData['type'] as String?;
          localSession.startedAt = serverData['startedAt'] != null
              ? DateTime.parse(serverData['startedAt'] as String)
              : null;
          localSession.completedAt = serverData['completedAt'] != null
              ? DateTime.parse(serverData['completedAt'] as String)
              : null;
          localSession.pausedAt = serverData['pausedAt'] != null
              ? DateTime.parse(serverData['pausedAt'] as String)
              : null;
          localSession.lastModifiedServer = serverModified;
          localSession.isSynced = true;
          localSession.syncStatus = 'synced';
          localSession.syncRetryCount = 0;
          localSession.syncError = null;
          localSession.lastSyncAttempt = DateTime.now();
          await db.localSessions.put(localSession);
        });

        debugPrint('  ✅ Local session updated from server (conflict resolved)');
        return;
      }

      // No conflict - update server with local changes
      await _apiService.patch<void>(
        ApiConfig.sessionStatus(localSession.serverId!),
        data: {'status': localSession.status},
      );

      // Mark as synced
      await db.writeTxn(() async {
        localSession.isSynced = true;
        localSession.syncStatus = 'synced';
        localSession.lastModifiedServer = DateTime.now();
        localSession.syncRetryCount = 0;
        localSession.syncError = null;
        localSession.lastSyncAttempt = DateTime.now();
        await db.localSessions.put(localSession);
      });

      debugPrint('  ✅ Session updated on server');
    } catch (e) {
      rethrow;
    }
  }

  /// Sync a session that needs to be deleted from the server
  Future<void> _syncDeleteSession(Isar db, LocalSession localSession) async {
    if (localSession.serverId == null) {
      // Never synced to server - just delete locally
      await db.writeTxn(() async {
        await db.localSessions.delete(localSession.localId);
      });
      debugPrint('  ✅ Local-only session deleted');
      return;
    }

    debugPrint('  Deleting session ${localSession.serverId} from server...');

    try {
      // DELETE from server
      await _apiService.delete(ApiConfig.sessionById(localSession.serverId!));

      // Delete from local database
      await db.writeTxn(() async {
        await db.localSessions.delete(localSession.localId);
      });

      debugPrint('  ✅ Session deleted from server and locally');
    } catch (e) {
      rethrow;
    }
  }

  /// Mark sync error with exponential backoff
  Future<void> _markSyncError(Isar db, LocalSession session, String error) async {
    await db.writeTxn(() async {
      session.syncRetryCount += 1;
      session.syncError = error;
      session.lastSyncAttempt = DateTime.now();

      if (session.syncRetryCount >= _maxRetries) {
        session.syncStatus = 'sync_error';
        debugPrint('  ❌ Session ${session.localId} failed after $_maxRetries attempts: $error');
      } else {
        debugPrint('  ⚠️ Session ${session.localId} sync failed (attempt ${session.syncRetryCount}/$_maxRetries): $error');
      }

      await db.localSessions.put(session);
    });
  }

  /// Get sync status summary
  Future<Map<String, dynamic>> getSyncStatus() async {
    final db = _localDb.database;

    final pendingCount = await db.localSessions
        .filter()
        .isSyncedEqualTo(false)
        .count();

    final errorCount = await db.localSessions
        .filter()
        .syncStatusEqualTo('sync_error')
        .count();

    final allSessions = await db.localSessions.where().findAll();
    final lastSyncAttempts = allSessions
        .where((s) => s.lastSyncAttempt != null)
        .map((s) => s.lastSyncAttempt!)
        .toList();

    final lastSyncTime = lastSyncAttempts.isEmpty
        ? null
        : lastSyncAttempts.reduce((a, b) => a.isAfter(b) ? a : b);

    return {
      'isSyncing': _isSyncing,
      'pendingCount': pendingCount,
      'errorCount': errorCount,
      'lastSyncTime': lastSyncTime,
      'isOnline': _connectivity.isOnline,
    };
  }

  /// Retry failed syncs
  Future<void> retryFailedSyncs() async {
    final db = _localDb.database;

    // Reset retry count for failed items
    final failedSessions = await db.localSessions
        .filter()
        .syncStatusEqualTo('sync_error')
        .findAll();

    await db.writeTxn(() async {
      for (final session in failedSessions) {
        session.syncRetryCount = 0;
        session.syncStatus = 'pending_update'; // Reset to pending
        session.syncError = null;
        await db.localSessions.put(session);
      }
    });

    debugPrint('🔄 Retrying ${failedSessions.length} failed syncs');

    // Trigger immediate sync
    if (failedSessions.isNotEmpty) {
      await sync();
    }
  }

  /// Dispose resources
  void dispose() {
    _periodicSyncTimer?.cancel();
    _connectivitySubscription?.cancel();
    _debounceTimer?.cancel();
    _isInitialized = false;
    debugPrint('🛑 SyncService disposed');
  }

  /// Reset singleton (useful for testing)
  static void reset() {
    _instance?.dispose();
    _instance = null;
  }
}
