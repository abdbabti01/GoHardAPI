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
import '../local/models/local_exercise.dart';
import '../local/models/local_exercise_set.dart';
import '../local/models/local_exercise_template.dart';

/// Repository for session (workout) operations with offline support
class SessionRepository {
  final ApiService _apiService;
  final LocalDatabaseService _localDb;
  final ConnectivityService _connectivity;

  SessionRepository(this._apiService, this._localDb, this._connectivity);

  /// Get all sessions for the current user
  /// Offline-first: returns local cache, then tries to sync with server
  Future<List<Session>> getSessions() async {
    final Isar db = _localDb.database;

    if (_connectivity.isOnline) {
      try {
        // Fetch from API
        final data = await _apiService.get<List<dynamic>>(ApiConfig.sessions);
        final apiSessions =
            data
                .map((json) => Session.fromJson(json as Map<String, dynamic>))
                .toList();

        // Update local cache (sessions AND their exercises)
        await db.writeTxn(() async {
          for (final apiSession in apiSessions) {
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

  /// Get sessions from local database with exercises
  Future<List<Session>> _getLocalSessions(Isar db) async {
    final localSessions = await db.localSessions.where().findAll();

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

    return ModelMapper.localToSession(localSession, exercises: exercises);
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
    localSession ??=
        await db.localSessions.filter().serverIdEqualTo(id).findFirst();

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

        debugPrint(
          '✅ Updated session status on server: ${localSession.serverId}',
        );
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
        // Only mark as pending_update if session already exists on server
        // If no serverId, keep it as pending_create
        if (localSession.serverId != null) {
          localSession.syncStatus = 'pending_update';
        }
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
    localSession ??=
        await db.localSessions.filter().serverIdEqualTo(id).findFirst();

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
        // Only mark as pending_update if session already exists on server
        // If no serverId, keep it as pending_create
        if (localSession.serverId != null) {
          localSession.syncStatus = 'pending_update';
        }
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
