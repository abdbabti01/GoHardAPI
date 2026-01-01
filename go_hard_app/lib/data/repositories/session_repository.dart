import 'package:flutter/foundation.dart';
import 'package:isar/isar.dart';
import '../../core/constants/api_config.dart';
import '../../core/services/connectivity_service.dart';
import '../models/session.dart';
import '../models/exercise.dart';
import '../services/api_service.dart';
import '../local/services/local_database_service.dart';
import '../local/services/model_mapper.dart';
import '../local/models/local_session.dart';

/// Repository for session (workout) operations with offline support
class SessionRepository {
  final ApiService _apiService;
  final LocalDatabaseService _localDb;
  final ConnectivityService _connectivity;

  SessionRepository(
    this._apiService,
    this._localDb,
    this._connectivity,
  );

  /// Get all sessions for the current user
  /// Offline-first: returns local cache, then tries to sync with server
  Future<List<Session>> getSessions() async {
    final Isar db = _localDb.database;

    if (_connectivity.isOnline) {
      try {
        // Fetch from API
        final data = await _apiService.get<List<dynamic>>(ApiConfig.sessions);
        final apiSessions = data
            .map((json) => Session.fromJson(json as Map<String, dynamic>))
            .toList();

        // Update local cache
        await db.writeTxn(() async {
          for (final apiSession in apiSessions) {
            // Check if session already exists locally
            final existingLocal = await db.localSessions
                .filter()
                .serverIdEqualTo(apiSession.id)
                .findFirst();

            if (existingLocal != null) {
              // Update existing local session
              final updated = ModelMapper.sessionToLocal(
                apiSession,
                localId: existingLocal.localId,
                isSynced: true,
              );
              await db.localSessions.put(updated);
            } else {
              // Create new local session
              final localSession = ModelMapper.sessionToLocal(apiSession);
              await db.localSessions.put(localSession);
            }
          }
        });

        debugPrint('✅ Synced ${apiSessions.length} sessions from server');
        return apiSessions;
      } catch (e) {
        debugPrint('⚠️ API failed, falling back to local cache: $e');
        return await _getLocalSessions(db);
      }
    } else {
      debugPrint('📴 Offline - returning cached sessions');
      return await _getLocalSessions(db);
    }
  }

  /// Get sessions from local database
  Future<List<Session>> _getLocalSessions(Isar db) async {
    final localSessions = await db.localSessions.where().findAll();
    return localSessions
        .map((local) => ModelMapper.localToSession(local))
        .toList();
  }

  /// Get session by ID
  Future<Session> getSession(int id) async {
    final data = await _apiService.get<Map<String, dynamic>>(
      ApiConfig.sessionById(id),
    );
    return Session.fromJson(data);
  }

  /// Create new session
  /// Optimistic update: saves locally first, syncs to server if online
  Future<Session> createSession(Session session) async {
    final db = _localDb.database;

    if (_connectivity.isOnline) {
      try {
        // Try API first when online
        final data = await _apiService.post<Map<String, dynamic>>(
          ApiConfig.sessions,
          data: session.toJson(),
        );
        final apiSession = Session.fromJson(data);

        // Save to local cache with synced status
        await db.writeTxn(() async {
          final localSession = ModelMapper.sessionToLocal(apiSession);
          await db.localSessions.put(localSession);
        });

        debugPrint('✅ Created session on server: ${apiSession.id}');
        return apiSession;
      } catch (e) {
        debugPrint('⚠️ API failed, saving locally: $e');
        return await _createLocalSession(session, db, isPending: true);
      }
    } else {
      debugPrint('📴 Offline - saving session locally');
      return await _createLocalSession(session, db, isPending: true);
    }
  }

  /// Create session in local database
  Future<Session> _createLocalSession(
    Session session,
    Isar db, {
    required bool isPending,
  }) async {
    final localSession = LocalSession(
      serverId: isPending ? null : session.id,
      userId: session.userId,
      date: session.date,
      duration: session.duration,
      notes: session.notes,
      type: session.type,
      status: session.status,
      startedAt: session.startedAt,
      completedAt: session.completedAt,
      pausedAt: session.pausedAt,
      isSynced: !isPending,
      syncStatus: isPending ? 'pending_create' : 'synced',
      lastModifiedLocal: DateTime.now(),
    );

    await db.writeTxn(() => db.localSessions.put(localSession));

    debugPrint('💾 Saved session locally: ${localSession.localId}');

    // Return with local ID (temporary until synced)
    return Session(
      id: localSession.localId, // Use local ID temporarily
      userId: localSession.userId,
      date: localSession.date,
      duration: localSession.duration,
      notes: localSession.notes,
      type: localSession.type,
      status: localSession.status,
      startedAt: localSession.startedAt,
      completedAt: localSession.completedAt,
      pausedAt: localSession.pausedAt,
    );
  }

  /// Update session status
  /// Optimistic update: updates locally first, syncs to server if online
  Future<Session> updateSessionStatus(int id, String status) async {
    final Isar db = _localDb.database;

    // Find local session (id could be localId or serverId)
    var localSession = await db.localSessions.get(id);
    localSession ??= await db.localSessions
          .filter()
          .serverIdEqualTo(id)
          .findFirst();

    if (localSession == null) {
      throw Exception('Session not found: $id');
    }

    if (_connectivity.isOnline && localSession.serverId != null) {
      try {
        // Update on server
        await _apiService.patch<void>(
          ApiConfig.sessionStatus(localSession.serverId!),
          data: {'status': status},
        );

        // Update local with synced status
        await db.writeTxn(() async {
          localSession!.status = status;
          localSession.lastModifiedLocal = DateTime.now();
          localSession.isSynced = true;
          localSession.syncStatus = 'synced';
          await db.localSessions.put(localSession);
        });

        debugPrint('✅ Updated session status on server: ${localSession.serverId}');
      } catch (e) {
        debugPrint('⚠️ API failed, updating locally: $e');
        await _updateLocalSessionStatus(db, localSession, status);
      }
    } else {
      debugPrint('📴 Offline - updating session status locally');
      await _updateLocalSessionStatus(db, localSession, status);
    }

    return ModelMapper.localToSession(localSession);
  }

  /// Update session status in local database
  Future<void> _updateLocalSessionStatus(
    Isar db,
    LocalSession localSession,
    String status,
  ) async {
    await db.writeTxn(() async {
      localSession.status = status;
      localSession.lastModifiedLocal = DateTime.now();
      localSession.isSynced = false;
      localSession.syncStatus = 'pending_update';
      await db.localSessions.put(localSession);
    });
  }

  /// Pause session timer
  /// Works offline by updating local database
  Future<void> pauseSession(int id) async {
    final Isar db = _localDb.database;
    var localSession = await db.localSessions.get(id);
    localSession ??= await db.localSessions
          .filter()
          .serverIdEqualTo(id)
          .findFirst();

    if (localSession == null) {
      throw Exception('Session not found: $id');
    }

    if (_connectivity.isOnline && localSession.serverId != null) {
      try {
        await _apiService.patch<void>(
          '${ApiConfig.sessions}/${localSession.serverId}/pause',
          data: {},
        );
      } catch (e) {
        debugPrint('⚠️ Pause API failed, will sync later: $e');
      }
    }

    // Always update locally
    await db.writeTxn(() async {
      localSession!.pausedAt = DateTime.now();
      localSession.lastModifiedLocal = DateTime.now();
      if (!_connectivity.isOnline || localSession.serverId == null) {
        localSession.isSynced = false;
        localSession.syncStatus = 'pending_update';
      }
      await db.localSessions.put(localSession);
    });

    debugPrint('⏸️ Session paused locally');
  }

  /// Resume session timer
  /// Works offline by updating local database
  Future<void> resumeSession(int id) async {
    final Isar db = _localDb.database;
    var localSession = await db.localSessions.get(id);
    localSession ??= await db.localSessions
          .filter()
          .serverIdEqualTo(id)
          .findFirst();

    if (localSession == null) {
      throw Exception('Session not found: $id');
    }

    if (_connectivity.isOnline && localSession.serverId != null) {
      try {
        await _apiService.patch<void>(
          '${ApiConfig.sessions}/${localSession.serverId}/resume',
          data: {},
        );
      } catch (e) {
        debugPrint('⚠️ Resume API failed, will sync later: $e');
      }
    }

    // Always update locally
    await db.writeTxn(() async {
      localSession!.pausedAt = null;
      localSession.lastModifiedLocal = DateTime.now();
      if (!_connectivity.isOnline || localSession.serverId == null) {
        localSession.isSynced = false;
        localSession.syncStatus = 'pending_update';
      }
      await db.localSessions.put(localSession);
    });

    debugPrint('▶️ Session resumed locally');
  }

  /// Delete session
  /// Marks as pending_delete offline, deletes from server when online
  Future<bool> deleteSession(int id) async {
    final Isar db = _localDb.database;
    var localSession = await db.localSessions.get(id);
    localSession ??= await db.localSessions
          .filter()
          .serverIdEqualTo(id)
          .findFirst();

    if (localSession == null) {
      throw Exception('Session not found: $id');
    }

    if (_connectivity.isOnline && localSession.serverId != null) {
      try {
        // Delete from server
        final success = await _apiService.delete(
          ApiConfig.sessionById(localSession.serverId!),
        );

        if (success) {
          // Delete from local database
          await db.writeTxn(() async {
            await db.localSessions.delete(localSession!.localId);
          });
          debugPrint('✅ Deleted session from server: ${localSession.serverId}');
          return true;
        }
        return false;
      } catch (e) {
        debugPrint('⚠️ Delete API failed, marking as pending: $e');
        await _markForDeletion(db, localSession);
        return true;
      }
    } else {
      debugPrint('📴 Offline - marking session for deletion');
      await _markForDeletion(db, localSession);
      return true;
    }
  }

  /// Mark session for deletion (to be synced later)
  Future<void> _markForDeletion(Isar db, LocalSession localSession) async {
    await db.writeTxn(() async {
      if (localSession.serverId == null) {
        // Never synced to server - safe to delete immediately
        await db.localSessions.delete(localSession.localId);
      } else {
        // Synced before - mark for deletion
        localSession.isSynced = false;
        localSession.syncStatus = 'pending_delete';
        localSession.lastModifiedLocal = DateTime.now();
        await db.localSessions.put(localSession);
      }
    });
  }

  /// Add exercise to session
  Future<Exercise> addExerciseToSession(
    int sessionId,
    int exerciseTemplateId,
  ) async {
    // Send exerciseTemplateId as object with 'exerciseTemplateId' property
    final data = await _apiService.post<Map<String, dynamic>>(
      ApiConfig.sessionExercises(sessionId),
      data: {'exerciseTemplateId': exerciseTemplateId},
    );
    return Exercise.fromJson(data);
  }
}
