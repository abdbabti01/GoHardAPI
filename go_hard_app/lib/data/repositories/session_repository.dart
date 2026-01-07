import 'package:flutter/foundation.dart';
import 'package:isar/isar.dart';
import '../../core/constants/api_config.dart';
import '../../core/services/connectivity_service.dart';
import '../models/session.dart';
import '../models/exercise.dart';
import '../services/api_service.dart';
import '../services/auth_service.dart';
import '../local/services/local_database_service.dart';
import '../local/services/model_mapper.dart';
import '../local/models/local_session.dart';
import '../local/models/local_exercise.dart';
import '../local/models/local_exercise_set.dart';
import '../local/models/local_exercise_template.dart';

/// Repository for session (workout) operations with offline support
class SessionRepository {
  final ApiService _apiService;
  final LocalDatabaseService _localDb;
  final ConnectivityService _connectivity;
  final AuthService _authService;

  SessionRepository(
    this._apiService,
    this._localDb,
    this._connectivity,
    this._authService,
  );

  /// Get all sessions for the current user
  /// Offline-first: returns local cache immediately, syncs with server in background
  /// Set [waitForSync] to true to wait for server sync before returning (useful after login)
  Future<List<Session>> getSessions({bool waitForSync = false}) async {
    final Isar db = _localDb.database;

    // If waitForSync is true and we're online, sync first then return fresh data
    if (waitForSync && _connectivity.isOnline) {
      debugPrint('⏳ Waiting for server sync before returning sessions...');
      await _syncSessionsFromServer(db);
      final freshSessions = await _getLocalSessions(db);
      return freshSessions;
    }

    // Otherwise, use offline-first approach: load from cache first for instant response
    final cachedSessions = await _getLocalSessions(db);

    // Then sync with server in background if online (don't block)
    if (_connectivity.isOnline) {
      _syncSessionsFromServer(db)
          .then((_) {
            debugPrint('✅ Background sync: Sessions synced from server');
          })
          .catchError((e) {
            debugPrint('⚠️ Background sync failed: $e');
          });
    }

    return cachedSessions;
  }

  /// Background sync: Fetch sessions from server and update cache
  Future<void> _syncSessionsFromServer(Isar db) async {
    try {
      // Fetch from API
      final data = await _apiService.get<List<dynamic>>(ApiConfig.sessions);
      final apiSessions =
          data
              .map((json) => Session.fromJson(json as Map<String, dynamic>))
              .toList();

      // Get current user ID for filtering
      final currentUserId = await _authService.getUserId();
      if (currentUserId == null) {
        debugPrint('⚠️ No authenticated user, skipping cache update');
        return; // Exit background sync, cache already returned to caller
      }

      // Update local cache (sessions AND their exercises)
      await db.writeTxn(() async {
        for (final apiSession in apiSessions) {
          // Only cache sessions belonging to current user
          if (apiSession.userId != currentUserId) {
            debugPrint(
              '  ⏭️ Skipping session ${apiSession.id} - belongs to different user (${apiSession.userId} != $currentUserId)',
            );
            continue;
          }

          // Check if session already exists locally
          final existingLocal =
              await db.localSessions
                  .filter()
                  .serverIdEqualTo(apiSession.id)
                  .findFirst();

          // Skip sessions marked for deletion - they should not be re-cached
          if (existingLocal != null &&
              existingLocal.syncStatus == 'pending_delete') {
            debugPrint(
              '  ⏭️ Skipping session ${apiSession.id} - marked for deletion',
            );
            continue;
          }

          LocalSession savedSession;
          if (existingLocal != null) {
            // Update existing local session
            final updated = ModelMapper.sessionToLocal(
              apiSession,
              localId: existingLocal.localId,
              isSynced: true,
            );
            await db.localSessions.put(updated);
            savedSession = updated;
          } else {
            // Create new local session
            final localSession = ModelMapper.sessionToLocal(apiSession);
            await db.localSessions.put(localSession);
            savedSession = localSession;
          }

          // Save exercises for this session
          int exerciseCount = 0;
          for (final apiExercise in apiSession.exercises) {
            // Check if exercise already exists locally
            final existingExercise =
                await db.localExercises
                    .filter()
                    .serverIdEqualTo(apiExercise.id)
                    .findFirst();

            if (existingExercise != null) {
              // Update existing
              final updated = ModelMapper.exerciseToLocal(
                apiExercise,
                sessionLocalId: savedSession.localId,
                localId: existingExercise.localId,
                isSynced: true,
              );
              await db.localExercises.put(updated);
              debugPrint(
                '    ✏️ Updated exercise ${updated.serverId}, sessionLocalId=${updated.sessionLocalId}',
              );
            } else {
              // Create new
              final localExercise = ModelMapper.exerciseToLocal(
                apiExercise,
                sessionLocalId: savedSession.localId,
              );
              final savedExerciseId = await db.localExercises.put(
                localExercise,
              );
              debugPrint(
                '    ➕ Created exercise ${localExercise.serverId}, localId=$savedExerciseId, sessionLocalId=${localExercise.sessionLocalId}',
              );
            }
            exerciseCount++;
          }
          debugPrint(
            '  📝 Cached $exerciseCount exercises for session ${apiSession.id}',
          );
        }
      });

      debugPrint('✅ Synced ${apiSessions.length} sessions from server');
    } catch (e) {
      // Sync failed - but that's okay, we already returned cached data
      rethrow; // Let the caller handle the error
    }
  }

  /// Get sessions from local database with exercises
  Future<List<Session>> _getLocalSessions(Isar db) async {
    // Get current user ID to filter sessions
    final userId = await _authService.getUserId();
    if (userId == null) {
      debugPrint('⚠️ No authenticated user, returning empty list');
      return [];
    }

    // Filter sessions by current user only
    final localSessions =
        await db.localSessions.filter().userIdEqualTo(userId).findAll();

    final sessions = <Session>[];
    for (final localSession in localSessions) {
      // Skip sessions marked for deletion
      if (localSession.syncStatus == 'pending_delete') {
        continue;
      }

      // Load exercises for this session
      final localExercises =
          await db.localExercises
              .filter()
              .sessionLocalIdEqualTo(localSession.localId)
              .findAll();

      final exercises =
          localExercises
              .map((localEx) => ModelMapper.localToExercise(localEx))
              .toList();

      sessions.add(
        ModelMapper.localToSession(localSession, exercises: exercises),
      );
    }

    return sessions;
  }

  /// Get session by ID
  /// Offline-first: returns local cache, then tries to sync with server
  Future<Session> getSession(int id) async {
    final Isar db = _localDb.database;

    // Check if there's a local version with pending changes
    var localSession =
        await db.localSessions.filter().serverIdEqualTo(id).findFirst();
    localSession ??= await db.localSessions.get(id);

    // If local session has pending changes, return it instead of fetching from server
    if (localSession != null && !localSession.isSynced) {
      debugPrint('📝 Session has pending changes, returning local version');
      return await _getLocalSession(db, id);
    }

    if (_connectivity.isOnline) {
      try {
        // Fetch from API
        final data = await _apiService.get<Map<String, dynamic>>(
          ApiConfig.sessionById(id),
        );
        final apiSession = Session.fromJson(data);

        // Update local cache (session AND exercises)
        await db.writeTxn(() async {
          final existingLocal =
              await db.localSessions
                  .filter()
                  .serverIdEqualTo(apiSession.id)
                  .findFirst();

          LocalSession savedSession;
          if (existingLocal != null) {
            final updated = ModelMapper.sessionToLocal(
              apiSession,
              localId: existingLocal.localId,
              isSynced: true,
            );
            await db.localSessions.put(updated);
            savedSession = updated;
          } else {
            final localSession = ModelMapper.sessionToLocal(apiSession);
            await db.localSessions.put(localSession);
            savedSession = localSession;
          }

          // Save exercises for this session
          for (final apiExercise in apiSession.exercises) {
            final existingExercise =
                await db.localExercises
                    .filter()
                    .serverIdEqualTo(apiExercise.id)
                    .findFirst();

            if (existingExercise != null) {
              final updated = ModelMapper.exerciseToLocal(
                apiExercise,
                sessionLocalId: savedSession.localId,
                localId: existingExercise.localId,
                isSynced: true,
              );
              await db.localExercises.put(updated);
            } else {
              final localExercise = ModelMapper.exerciseToLocal(
                apiExercise,
                sessionLocalId: savedSession.localId,
              );
              await db.localExercises.put(localExercise);
            }
          }
        });

        return apiSession;
      } catch (e) {
        debugPrint('⚠️ API failed, falling back to local cache: $e');
        return await _getLocalSession(db, id);
      }
    } else {
      debugPrint('📴 Offline - returning cached session');
      return await _getLocalSession(db, id);
    }
  }

  /// Get session from local database by ID (server ID or local ID) with exercises
  Future<Session> _getLocalSession(Isar db, int id) async {
    // Try by server ID first
    var localSession =
        await db.localSessions.filter().serverIdEqualTo(id).findFirst();

    // If not found, try by local ID
    localSession ??= await db.localSessions.get(id);

    if (localSession == null) {
      throw Exception('Session not found: $id');
    }

    // Load exercises for this session
    final localExercises =
        await db.localExercises
            .filter()
            .sessionLocalIdEqualTo(localSession.localId)
            .findAll();

    final exercises =
        localExercises
            .map((localEx) => ModelMapper.localToExercise(localEx))
            .toList();

    debugPrint(
      '  📦 Loaded session ${localSession.serverId ?? localSession.localId} from cache with ${exercises.length} exercises',
    );
    debugPrint('  📦 LocalSession startedAt: ${localSession.startedAt}');
    debugPrint('  📦 LocalSession pausedAt: ${localSession.pausedAt}');
    debugPrint('  📦 LocalSession status: ${localSession.status}');

    final session = ModelMapper.localToSession(
      localSession,
      exercises: exercises,
    );
    debugPrint('  📦 Mapped Session startedAt: ${session.startedAt}');
    debugPrint('  📦 Mapped Session pausedAt: ${session.pausedAt}');

    return session;
  }

  /// Create new session
  /// Optimistic update: saves locally first, syncs to server if online
  Future<Session> createSession(Session session) async {
    final db = _localDb.database;

    // ALWAYS create locally first for instant response
    final localResult = await _createLocalSession(session, db, isPending: true);

    // Then sync to server in background if online (don't block)
    if (_connectivity.isOnline) {
      _syncCreateSessionToServer(session, db, localResult.id)
          .then((_) {
            debugPrint('✅ Background sync: Created session on server');
          })
          .catchError((e) {
            debugPrint('⚠️ Background sync failed, will retry later: $e');
          });
    } else {
      debugPrint('📴 Offline - session will sync later');
    }

    return localResult;
  }

  /// Background sync: Create session on server
  Future<void> _syncCreateSessionToServer(
    Session session,
    Isar db,
    int localId,
  ) async {
    try {
      final data = await _apiService.post<Map<String, dynamic>>(
        ApiConfig.sessions,
        data: session.toJson(),
      );
      final apiSession = Session.fromJson(data);

      // Update local session with server ID
      await db.writeTxn(() async {
        final localSession = await db.localSessions.get(localId);
        if (localSession != null) {
          localSession.serverId = apiSession.id;
          localSession.isSynced = true;
          localSession.syncStatus = 'synced';
          await db.localSessions.put(localSession);
        }
      });
    } catch (e) {
      rethrow; // Let caller handle error
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
      name: session.name,
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
      name: localSession.name,
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
    localSession ??=
        await db.localSessions.filter().serverIdEqualTo(id).findFirst();

    if (localSession == null) {
      throw Exception('Session not found: $id');
    }

    // ALWAYS update locally first for instant response
    await _updateLocalSessionStatus(db, localSession, status);

    // Then sync to server in background if online (don't block)
    if (_connectivity.isOnline && localSession.serverId != null) {
      _syncSessionStatusToServer(db, localSession.serverId!, localSession)
          .then((_) {
            debugPrint('✅ Background sync: Updated session status on server');
          })
          .catchError((e) {
            debugPrint('⚠️ Background sync failed, will retry later: $e');
          });
    } else {
      debugPrint('📴 Offline - session status will sync later');
    }

    // Load exercises to include in returned session
    final localExercises =
        await db.localExercises
            .filter()
            .sessionLocalIdEqualTo(localSession.localId)
            .findAll();

    final exercises =
        localExercises
            .map((localEx) => ModelMapper.localToExercise(localEx))
            .toList();

    return ModelMapper.localToSession(localSession, exercises: exercises);
  }

  /// Background sync: Update session status on server
  Future<void> _syncSessionStatusToServer(
    Isar db,
    int serverId,
    LocalSession localSession,
  ) async {
    try {
      // Send status AND timestamps to server (preserves timer state)
      // Timestamps are already in UTC format from storage
      final data = {
        'status': localSession.status,
        if (localSession.startedAt != null)
          'startedAt': localSession.startedAt!.toIso8601String(),
        if (localSession.completedAt != null)
          'completedAt': localSession.completedAt!.toIso8601String(),
        if (localSession.pausedAt != null)
          'pausedAt': localSession.pausedAt!.toIso8601String(),
      };

      await _apiService.patch<void>(
        ApiConfig.sessionStatus(serverId),
        data: data,
      );

      debugPrint(
        '✅ Synced session $serverId with timestamps (startedAt: ${localSession.startedAt})',
      );

      // Update sync status in local DB
      await db.writeTxn(() async {
        final session =
            await db.localSessions
                .filter()
                .serverIdEqualTo(serverId)
                .findFirst();
        if (session != null) {
          session.isSynced = true;
          session.syncStatus = 'synced';
          await db.localSessions.put(session);
        }
      });
    } catch (e) {
      rethrow; // Let caller handle error
    }
  }

  /// Update session status in local database
  Future<void> _updateLocalSessionStatus(
    Isar db,
    LocalSession localSession,
    String status,
  ) async {
    await db.writeTxn(() async {
      final now = DateTime.now();
      final nowUtc = DateTime.now().toUtc(); // Use UTC for timestamps
      localSession.status = status;
      localSession.lastModifiedLocal = now;
      localSession.isSynced = false;

      // Set startedAt when status changes to 'in_progress'
      if (status == 'in_progress' && localSession.startedAt == null) {
        localSession.startedAt = nowUtc; // Store as UTC
        localSession.pausedAt = null; // Clear any pause state
        debugPrint('🏋️ Set startedAt in DB (UTC): $nowUtc');
      }

      // Set completedAt when status changes to 'completed'
      if (status == 'completed' && localSession.completedAt == null) {
        localSession.completedAt = nowUtc; // Store as UTC
        debugPrint('✅ Set completedAt in DB (UTC): $nowUtc');
      }

      // Only mark as pending_update if session already exists on server
      // If no serverId, keep it as pending_create
      if (localSession.serverId != null) {
        localSession.syncStatus = 'pending_update';
      }
      await db.localSessions.put(localSession);
    });
  }

  /// Pause session timer
  /// Works offline by updating local database
  Future<void> pauseSession(int id) async {
    final Isar db = _localDb.database;
    var localSession = await db.localSessions.get(id);
    localSession ??=
        await db.localSessions.filter().serverIdEqualTo(id).findFirst();

    if (localSession == null) {
      throw Exception('Session not found: $id');
    }

    // ALWAYS update locally first for instant response
    await db.writeTxn(() async {
      final nowUtc = DateTime.now().toUtc(); // CRITICAL: Store as UTC
      localSession!.pausedAt = nowUtc; // Store as UTC, not local time
      localSession.lastModifiedLocal = DateTime.now();
      localSession.isSynced = false;
      // Only mark as pending_update if session already exists on server
      if (localSession.serverId != null) {
        localSession.syncStatus = 'pending_update';
      }
      await db.localSessions.put(localSession);
      debugPrint('⏸️ Session paused locally (pausedAt UTC: $nowUtc)');
    });

    debugPrint('⏸️ Session paused locally');

    // Then sync to server in background if online (don't block)
    if (_connectivity.isOnline && localSession.serverId != null) {
      _apiService
          .patch<void>(
            '${ApiConfig.sessions}/${localSession.serverId}/pause',
            data: {},
          )
          .then((_) {
            debugPrint('✅ Background sync: Pause synced to server');
            // Update sync status
            db.writeTxn(() async {
              final session =
                  await db.localSessions
                      .filter()
                      .serverIdEqualTo(localSession!.serverId!)
                      .findFirst();
              if (session != null) {
                session.isSynced = true;
                session.syncStatus = 'synced';
                await db.localSessions.put(session);
              }
            });
          })
          .catchError((e) {
            debugPrint('⚠️ Background sync failed, will retry later: $e');
          });
    }
  }

  /// Resume session timer
  /// Works offline by updating local database
  Future<void> resumeSession(int id) async {
    final Isar db = _localDb.database;
    var localSession = await db.localSessions.get(id);
    localSession ??=
        await db.localSessions.filter().serverIdEqualTo(id).findFirst();

    if (localSession == null) {
      throw Exception('Session not found: $id');
    }

    // ALWAYS update locally first for instant response
    await db.writeTxn(() async {
      // Calculate pause duration and adjust startedAt (just like backend does)
      if (localSession!.pausedAt != null && localSession.startedAt != null) {
        final nowUtc = DateTime.now().toUtc();
        final pauseDuration = nowUtc.difference(localSession.pausedAt!);
        localSession.startedAt = localSession.startedAt!.add(pauseDuration);
        debugPrint('▶️ Adjusted startedAt by pause duration: $pauseDuration');
      }

      localSession.pausedAt = null;
      localSession.lastModifiedLocal = DateTime.now();
      localSession.isSynced = false;
      // Only mark as pending_update if session already exists on server
      if (localSession.serverId != null) {
        localSession.syncStatus = 'pending_update';
      }
      await db.localSessions.put(localSession);
    });

    debugPrint('▶️ Session resumed locally');

    // Then sync to server in background if online (don't block)
    if (_connectivity.isOnline && localSession.serverId != null) {
      _apiService
          .patch<void>(
            '${ApiConfig.sessions}/${localSession.serverId}/resume',
            data: {},
          )
          .then((_) {
            debugPrint('✅ Background sync: Resume synced to server');
            // Update sync status
            db.writeTxn(() async {
              final session =
                  await db.localSessions
                      .filter()
                      .serverIdEqualTo(localSession!.serverId!)
                      .findFirst();
              if (session != null) {
                session.isSynced = true;
                session.syncStatus = 'synced';
                await db.localSessions.put(session);
              }
            });
          })
          .catchError((e) {
            debugPrint('⚠️ Background sync failed, will retry later: $e');
          });
    }
  }

  /// Delete session
  /// Marks as pending_delete offline, deletes from server when online
  Future<bool> deleteSession(int id) async {
    final Isar db = _localDb.database;
    var localSession = await db.localSessions.get(id);
    localSession ??=
        await db.localSessions.filter().serverIdEqualTo(id).findFirst();

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
          // Delete from local database (including exercises and sets)
          await _deleteSessionAndRelatedData(db, localSession);
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
    if (localSession.serverId == null) {
      // Never synced to server - safe to delete immediately (with related data)
      await _deleteSessionAndRelatedData(db, localSession);
    } else {
      // Synced before - mark for deletion
      await db.writeTxn(() async {
        localSession.isSynced = false;
        localSession.syncStatus = 'pending_delete';
        localSession.lastModifiedLocal = DateTime.now();
        await db.localSessions.put(localSession);
      });
    }
  }

  /// Delete session and all related exercises and sets
  Future<void> _deleteSessionAndRelatedData(
    Isar db,
    LocalSession localSession,
  ) async {
    await db.writeTxn(() async {
      // Delete all exercises for this session
      final exercises =
          await db.localExercises
              .filter()
              .sessionLocalIdEqualTo(localSession.localId)
              .findAll();

      for (final exercise in exercises) {
        // Delete all sets for this exercise
        await db.localExerciseSets
            .filter()
            .exerciseLocalIdEqualTo(exercise.localId)
            .deleteAll();
      }

      // Delete all exercises
      await db.localExercises
          .filter()
          .sessionLocalIdEqualTo(localSession.localId)
          .deleteAll();

      // Delete the session
      await db.localSessions.delete(localSession.localId);
    });
  }

  /// Update session name
  /// Optimistic update: updates locally first, syncs to server if online
  Future<Session> updateSessionName(int id, String name) async {
    final Isar db = _localDb.database;

    // Find local session (id could be localId or serverId)
    var localSession = await db.localSessions.get(id);
    localSession ??=
        await db.localSessions.filter().serverIdEqualTo(id).findFirst();

    if (localSession == null) {
      throw Exception('Session not found: $id');
    }

    // ALWAYS update locally first for instant response
    await db.writeTxn(() async {
      localSession!.name = name;
      localSession.lastModifiedLocal = DateTime.now();
      localSession.isSynced = false;
      // Only mark as pending_update if session already exists on server
      if (localSession.serverId != null) {
        localSession.syncStatus = 'pending_update';
      }
      await db.localSessions.put(localSession);
    });

    debugPrint('✏️ Session name updated locally to: $name');

    // Then sync to server in background if online (don't block)
    if (_connectivity.isOnline && localSession.serverId != null) {
      _syncSessionNameToServer(db, localSession.serverId!, name)
          .then((_) {
            debugPrint('✅ Background sync: Updated session name on server');
          })
          .catchError((e) {
            debugPrint('⚠️ Background sync failed, will retry later: $e');
          });
    } else {
      debugPrint('📴 Offline - session name will sync later');
    }

    // Load exercises to include in returned session
    final localExercises =
        await db.localExercises
            .filter()
            .sessionLocalIdEqualTo(localSession.localId)
            .findAll();

    final exercises =
        localExercises
            .map((localEx) => ModelMapper.localToExercise(localEx))
            .toList();

    return ModelMapper.localToSession(localSession, exercises: exercises);
  }

  /// Update workout date (used when starting future planned workout early)
  /// Optimistic update: updates locally first, syncs to server if online
  Future<void> updateWorkoutDate(int id, DateTime newDate) async {
    final Isar db = _localDb.database;

    // Find local session (id could be localId or serverId)
    var localSession = await db.localSessions.get(id);
    localSession ??=
        await db.localSessions.filter().serverIdEqualTo(id).findFirst();

    if (localSession == null) {
      throw Exception('Session not found: $id');
    }

    // ALWAYS update locally first for instant response
    await db.writeTxn(() async {
      // Convert to date-only (no time component)
      final dateOnly = DateTime(newDate.year, newDate.month, newDate.day);
      localSession!.date = dateOnly;
      localSession.lastModifiedLocal = DateTime.now();
      localSession.isSynced = false;
      // Only mark as pending_update if session already exists on server
      if (localSession.serverId != null) {
        localSession.syncStatus = 'pending_update';
      }
      await db.localSessions.put(localSession);
    });

    debugPrint('📅 Workout date updated locally to: $newDate');

    // Then sync to server in background if online (don't block)
    if (_connectivity.isOnline && localSession.serverId != null) {
      _syncSessionDateToServer(db, localSession.serverId!, newDate)
          .then((_) {
            debugPrint('✅ Background sync: Updated workout date on server');
          })
          .catchError((e) {
            debugPrint('⚠️ Background sync failed, will retry later: $e');
          });
    } else {
      debugPrint('📴 Offline - workout date will sync later');
    }
  }

  /// Background sync: Update workout date on server
  Future<void> _syncSessionDateToServer(
    Isar db,
    int serverId,
    DateTime newDate,
  ) async {
    try {
      // Fetch the current session to send full data (required by PUT)
      final session =
          await db.localSessions.filter().serverIdEqualTo(serverId).findFirst();

      if (session == null) return;

      // Convert to API model
      final apiSession = ModelMapper.localToSession(session);
      final updateData = apiSession.toJson();

      await _apiService.put<void>(
        ApiConfig.sessionById(serverId),
        data: updateData,
      );

      // Mark as synced
      await db.writeTxn(() async {
        final updatedSession =
            await db.localSessions
                .filter()
                .serverIdEqualTo(serverId)
                .findFirst();
        if (updatedSession != null) {
          updatedSession.isSynced = true;
          updatedSession.syncStatus = 'synced';
          await db.localSessions.put(updatedSession);
        }
      });
    } catch (e) {
      debugPrint('Error syncing workout date to server: $e');
      rethrow;
    }
  }

  /// Background sync: Update session name on server
  Future<void> _syncSessionNameToServer(
    Isar db,
    int serverId,
    String name,
  ) async {
    try {
      // Fetch the current session to send full data (required by PUT)
      final session =
          await db.localSessions.filter().serverIdEqualTo(serverId).findFirst();

      if (session == null) return;

      // Convert to API model
      final apiSession = ModelMapper.localToSession(session);
      final updateData = apiSession.toJson();
      updateData['name'] = name; // Ensure name is updated

      await _apiService.put<void>(
        ApiConfig.sessionById(serverId),
        data: updateData,
      );

      // Update sync status in local DB
      await db.writeTxn(() async {
        final localSession =
            await db.localSessions
                .filter()
                .serverIdEqualTo(serverId)
                .findFirst();
        if (localSession != null) {
          localSession.isSynced = true;
          localSession.syncStatus = 'synced';
          await db.localSessions.put(localSession);
        }
      });
    } catch (e) {
      rethrow; // Let caller handle error
    }
  }

  /// Add exercise to session
  /// Works offline by creating locally and syncing later
  Future<Exercise> addExerciseToSession(
    int sessionId,
    int exerciseTemplateId,
  ) async {
    final Isar db = _localDb.database;

    // Find the local session
    var localSession =
        await db.localSessions.filter().serverIdEqualTo(sessionId).findFirst();
    localSession ??= await db.localSessions.get(sessionId);

    if (localSession == null) {
      throw Exception('Session not found: $sessionId');
    }

    if (_connectivity.isOnline && localSession.serverId != null) {
      try {
        // Try API first
        final data = await _apiService.post<Map<String, dynamic>>(
          ApiConfig.sessionExercises(localSession.serverId!),
          data: {'exerciseTemplateId': exerciseTemplateId},
        );
        final apiExercise = Exercise.fromJson(data);

        // Cache the exercise locally
        await db.writeTxn(() async {
          final localExercise = ModelMapper.exerciseToLocal(
            apiExercise,
            sessionLocalId: localSession!.localId,
            isSynced: true,
          );
          await db.localExercises.put(localExercise);
        });

        return apiExercise;
      } catch (e) {
        debugPrint('⚠️ Add exercise API failed, creating locally: $e');
        // Fall through to offline creation
      }
    }

    // Create exercise locally (offline or API failed)
    // Get exercise template name from local cache
    String exerciseName = 'Exercise'; // Default name
    try {
      final templates =
          await db.collection<LocalExerciseTemplate>().where().findAll();
      final template = templates.firstWhere(
        (t) => t.serverId == exerciseTemplateId,
        orElse: () => templates.first,
      );
      exerciseName = template.name;
    } catch (e) {
      debugPrint('⚠️ Could not find exercise template $exerciseTemplateId: $e');
    }

    int localId = 0;

    await db.writeTxn(() async {
      final tempExercise = Exercise(
        id: 0, // Temporary, will be replaced
        sessionId: sessionId,
        name: exerciseName,
        exerciseTemplateId: exerciseTemplateId,
        duration: null,
        restTime: null,
        notes: null,
        exerciseSets: [],
      );

      final localExercise = ModelMapper.exerciseToLocal(
        tempExercise,
        sessionLocalId: localSession!.localId,
        isSynced: false,
      );
      localId = await db.localExercises.put(localExercise);
    });

    // Return exercise with local ID and name
    final newExercise = Exercise(
      id: localId, // Use local ID temporarily
      sessionId: sessionId,
      name: exerciseName,
      exerciseTemplateId: exerciseTemplateId,
      duration: null,
      restTime: null,
      notes: null,
      exerciseSets: [],
    );

    debugPrint(
      '➕ Created exercise "$exerciseName" locally (offline), id=$localId, will sync later',
    );
    return newExercise;
  }
}
