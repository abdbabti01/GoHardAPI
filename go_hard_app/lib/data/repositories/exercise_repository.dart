import 'package:flutter/foundation.dart';
import 'package:isar/isar.dart';
import '../../core/constants/api_config.dart';
import '../../core/services/connectivity_service.dart';
import '../models/exercise_template.dart';
import '../models/exercise_set.dart';
import '../services/api_service.dart';
import '../local/services/local_database_service.dart';
import '../local/services/model_mapper.dart';
import '../local/models/local_exercise_template.dart';
import '../local/models/local_exercise.dart';
import '../local/models/local_exercise_set.dart';

/// Repository for exercise and exercise template operations with offline support
class ExerciseRepository {
  final ApiService _apiService;
  final LocalDatabaseService _localDb;
  final ConnectivityService _connectivity;

  ExerciseRepository(this._apiService, this._localDb, this._connectivity);

  // Exercise Templates

  /// Get all exercise templates with optional filtering
  /// Offline-first: returns local cache, then tries to sync with server
  Future<List<ExerciseTemplate>> getExerciseTemplates({
    String? category,
    String? muscleGroup,
    bool? isCustom,
  }) async {
    final Isar db = _localDb.database;

    if (_connectivity.isOnline) {
      try {
        final queryParams = <String, dynamic>{};
        if (category != null) queryParams['category'] = category;
        if (muscleGroup != null) queryParams['muscleGroup'] = muscleGroup;
        if (isCustom != null) queryParams['isCustom'] = isCustom;

        final data = await _apiService.get<List<dynamic>>(
          ApiConfig.exerciseTemplates,
          queryParameters: queryParams.isNotEmpty ? queryParams : null,
        );
        final apiTemplates =
            data
                .map(
                  (json) =>
                      ExerciseTemplate.fromJson(json as Map<String, dynamic>),
                )
                .toList();

        // Update local cache
        await db.writeTxn(() async {
          for (final apiTemplate in apiTemplates) {
            final localTemplate = ModelMapper.exerciseTemplateToLocal(
              apiTemplate,
              isSynced: true,
            );
            await db.localExerciseTemplates.put(localTemplate);
          }
        });

        debugPrint('✅ Cached ${apiTemplates.length} exercise templates');
        return apiTemplates;
      } catch (e) {
        debugPrint('⚠️ API failed, falling back to local cache: $e');
        return await _getLocalExerciseTemplates(
          db,
          category: category,
          muscleGroup: muscleGroup,
          isCustom: isCustom,
        );
      }
    } else {
      debugPrint('📴 Offline - returning cached exercise templates');
      return await _getLocalExerciseTemplates(
        db,
        category: category,
        muscleGroup: muscleGroup,
        isCustom: isCustom,
      );
    }
  }

  /// Get exercise templates from local database with optional filtering
  Future<List<ExerciseTemplate>> _getLocalExerciseTemplates(
    Isar db, {
    String? category,
    String? muscleGroup,
    bool? isCustom,
  }) async {
    // Get all local templates first
    List<LocalExerciseTemplate> localTemplates =
        await db.localExerciseTemplates.where().findAll();

    // Apply filters in memory
    if (category != null) {
      localTemplates =
          localTemplates.where((t) => t.category == category).toList();
    }
    if (muscleGroup != null) {
      localTemplates =
          localTemplates.where((t) => t.muscleGroup == muscleGroup).toList();
    }
    if (isCustom != null) {
      localTemplates =
          localTemplates.where((t) => t.isCustom == isCustom).toList();
    }

    return localTemplates
        .map((local) => ModelMapper.localToExerciseTemplate(local))
        .toList();
  }

  /// Get exercise template by ID
  Future<ExerciseTemplate> getExerciseTemplate(int id) async {
    final data = await _apiService.get<Map<String, dynamic>>(
      ApiConfig.exerciseTemplateById(id),
    );
    return ExerciseTemplate.fromJson(data);
  }

  /// Get all available categories
  /// Offline-first: returns distinct categories from local cache
  Future<List<String>> getCategories() async {
    final Isar db = _localDb.database;

    if (_connectivity.isOnline) {
      try {
        final data = await _apiService.get<List<dynamic>>(
          ApiConfig.exerciseTemplateCategories,
        );
        return data.map((e) => e.toString()).toList();
      } catch (e) {
        debugPrint('⚠️ API failed, using local categories: $e');
        return await _getLocalCategories(db);
      }
    } else {
      debugPrint('📴 Offline - returning cached categories');
      return await _getLocalCategories(db);
    }
  }

  Future<List<String>> _getLocalCategories(Isar db) async {
    final templates = await db.localExerciseTemplates.where().findAll();
    return templates
        .map((t) => t.category)
        .where((c) => c != null)
        .cast<String>()
        .toSet()
        .toList();
  }

  /// Get all available muscle groups
  /// Offline-first: returns distinct muscle groups from local cache
  Future<List<String>> getMuscleGroups() async {
    final Isar db = _localDb.database;

    if (_connectivity.isOnline) {
      try {
        final data = await _apiService.get<List<dynamic>>(
          ApiConfig.exerciseTemplateMuscleGroups,
        );
        return data.map((e) => e.toString()).toList();
      } catch (e) {
        debugPrint('⚠️ API failed, using local muscle groups: $e');
        return await _getLocalMuscleGroups(db);
      }
    } else {
      debugPrint('📴 Offline - returning cached muscle groups');
      return await _getLocalMuscleGroups(db);
    }
  }

  Future<List<String>> _getLocalMuscleGroups(Isar db) async {
    final templates = await db.localExerciseTemplates.where().findAll();
    return templates
        .map((t) => t.muscleGroup)
        .where((m) => m != null)
        .cast<String>()
        .toSet()
        .toList();
  }

  // Exercise Sets

  /// Get exercise sets by exercise ID
  /// Offline-first: returns local cache, then syncs with server if online
  Future<List<ExerciseSet>> getExerciseSets(int exerciseId) async {
    final Isar db = _localDb.database;

    // First, try to find the exercise in local database
    var localExercise =
        await db.localExercises
            .filter()
            .serverIdEqualTo(exerciseId)
            .findFirst();
    localExercise ??= await db.localExercises.get(exerciseId);

    if (localExercise == null) {
      // Exercise not found locally, try API if online
      if (_connectivity.isOnline) {
        final data = await _apiService.get<List<dynamic>>(
          ApiConfig.exerciseSetsByExerciseId(exerciseId),
        );
        return data
            .map((json) => ExerciseSet.fromJson(json as Map<String, dynamic>))
            .toList();
      }
      return []; // Offline and not cached
    }

    // Get sets from local database (exclude deleted ones)
    final localSets =
        await db.localExerciseSets
            .filter()
            .exerciseLocalIdEqualTo(localExercise.localId)
            .findAll();

    // Convert to API model and sort (filter out pending_delete)
    final sets =
        localSets
            .where((s) => s.syncStatus != 'pending_delete')
            .map((s) => ModelMapper.localToExerciseSet(s))
            .toList();
    sets.sort((a, b) => a.setNumber.compareTo(b.setNumber));

    // If online, fetch from server to check for updates
    if (_connectivity.isOnline && localExercise.serverId != null) {
      try {
        final data = await _apiService.get<List<dynamic>>(
          ApiConfig.exerciseSetsByExerciseId(localExercise.serverId!),
        );
        final apiSets =
            data
                .map(
                  (json) => ExerciseSet.fromJson(json as Map<String, dynamic>),
                )
                .toList();

        // Update local cache
        await db.writeTxn(() async {
          for (final apiSet in apiSets) {
            final existing =
                await db.localExerciseSets
                    .filter()
                    .serverIdEqualTo(apiSet.id)
                    .findFirst();

            if (existing != null) {
              final updated = ModelMapper.exerciseSetToLocal(
                apiSet,
                exerciseLocalId: localExercise!.localId,
                localId: existing.localId,
              );
              await db.localExerciseSets.put(updated);
            } else {
              final localSet = ModelMapper.exerciseSetToLocal(
                apiSet,
                exerciseLocalId: localExercise!.localId,
              );
              await db.localExerciseSets.put(localSet);
            }
          }
        });

        return apiSets;
      } catch (e) {
        debugPrint('⚠️ API failed, using local sets: $e');
        return sets;
      }
    }

    return sets;
  }

  /// Create new exercise set
  /// Offline-first: saves locally, syncs to server when online
  Future<ExerciseSet> createExerciseSet(ExerciseSet exerciseSet) async {
    final Isar db = _localDb.database;

    // Find the parent exercise
    var localExercise =
        await db.localExercises
            .filter()
            .serverIdEqualTo(exerciseSet.exerciseId)
            .findFirst();
    localExercise ??= await db.localExercises.get(exerciseSet.exerciseId);

    if (localExercise == null) {
      throw Exception('Exercise not found: ${exerciseSet.exerciseId}');
    }

    if (_connectivity.isOnline && localExercise.serverId != null) {
      try {
        // Try API first when online
        final data = await _apiService.post<Map<String, dynamic>>(
          ApiConfig.exerciseSets,
          data: exerciseSet.toJson(),
        );
        final apiSet = ExerciseSet.fromJson(data);

        // Save to local cache
        await db.writeTxn(() async {
          final localSet = ModelMapper.exerciseSetToLocal(
            apiSet,
            exerciseLocalId: localExercise!.localId,
          );
          await db.localExerciseSets.put(localSet);
        });

        debugPrint('✅ Created set on server');
        return apiSet;
      } catch (e) {
        debugPrint('⚠️ API failed, saving set locally: $e');
        return await _createLocalSet(db, exerciseSet, localExercise);
      }
    } else {
      debugPrint('📴 Offline - saving set locally');
      return await _createLocalSet(db, exerciseSet, localExercise);
    }
  }

  /// Create exercise set in local database
  Future<ExerciseSet> _createLocalSet(
    Isar db,
    ExerciseSet exerciseSet,
    LocalExercise localExercise,
  ) async {
    final localSet = ModelMapper.exerciseSetToLocal(
      exerciseSet,
      exerciseLocalId: localExercise.localId,
      exerciseServerId: localExercise.serverId,
      isSynced: false,
    );

    int localId = 0;
    await db.writeTxn(() async {
      localId = await db.localExerciseSets.put(localSet);
    });

    return ExerciseSet(
      id: localId, // Use local ID temporarily
      exerciseId: localExercise.serverId ?? localExercise.localId,
      setNumber: exerciseSet.setNumber,
      reps: exerciseSet.reps,
      weight: exerciseSet.weight,
      duration: exerciseSet.duration,
      isCompleted: exerciseSet.isCompleted,
      completedAt: exerciseSet.completedAt,
      notes: exerciseSet.notes,
    );
  }

  /// Update exercise set
  Future<ExerciseSet> updateExerciseSet(int id, ExerciseSet exerciseSet) async {
    final data = await _apiService.put<Map<String, dynamic>>(
      ApiConfig.exerciseSetById(id),
      data: exerciseSet.toJson(),
    );
    return ExerciseSet.fromJson(data);
  }

  /// Mark exercise set as complete
  /// Offline-first: updates locally, syncs to server when online
  Future<ExerciseSet> completeExerciseSet(int id) async {
    final Isar db = _localDb.database;
    var localSet = await db.localExerciseSets.get(id);
    localSet ??=
        await db.localExerciseSets.filter().serverIdEqualTo(id).findFirst();

    if (localSet == null) {
      throw Exception('Exercise set not found: $id');
    }

    if (_connectivity.isOnline && localSet.serverId != null) {
      try {
        final data = await _apiService.patch<Map<String, dynamic>>(
          ApiConfig.exerciseSetComplete(localSet.serverId!),
          data: {},
        );
        if (data == null) {
          throw Exception('Failed to complete exercise set: No data returned');
        }
        final apiSet = ExerciseSet.fromJson(data);

        // Update local cache
        await db.writeTxn(() async {
          final updated = ModelMapper.exerciseSetToLocal(
            apiSet,
            exerciseLocalId: localSet!.exerciseLocalId,
            localId: localSet.localId,
          );
          await db.localExerciseSets.put(updated);
        });

        return apiSet;
      } catch (e) {
        debugPrint('⚠️ Complete API failed, will sync later: $e');
      }
    }

    // Always update locally
    await db.writeTxn(() async {
      localSet!.isCompleted = true;
      localSet.completedAt = DateTime.now().toUtc();
      localSet.lastModifiedLocal = DateTime.now();
      if (!_connectivity.isOnline || localSet.serverId == null) {
        localSet.isSynced = false;
        if (localSet.serverId != null) {
          localSet.syncStatus = 'pending_update';
        }
      }
      await db.localExerciseSets.put(localSet);
    });

    debugPrint('✅ Set marked as complete locally');
    return ModelMapper.localToExerciseSet(localSet);
  }

  /// Delete exercise set
  /// Offline-first: marks as pending_delete, deletes from server when online
  Future<bool> deleteExerciseSet(int id) async {
    final Isar db = _localDb.database;
    var localSet = await db.localExerciseSets.get(id);
    localSet ??=
        await db.localExerciseSets.filter().serverIdEqualTo(id).findFirst();

    if (localSet == null) {
      throw Exception('Exercise set not found: $id');
    }

    if (_connectivity.isOnline && localSet.serverId != null) {
      try {
        final success = await _apiService.delete(
          ApiConfig.exerciseSetById(localSet.serverId!),
        );
        if (success) {
          // Delete from local database
          await db.writeTxn(() async {
            await db.localExerciseSets.delete(localSet!.localId);
          });
          debugPrint('✅ Set deleted from server and locally');
          return true;
        }
        return false;
      } catch (e) {
        debugPrint('⚠️ Delete API failed, marking for deletion: $e');
      }
    }

    // Mark as pending_delete (or delete locally if never synced)
    if (localSet.serverId == null) {
      // Never synced to server - just delete locally
      await db.writeTxn(() async {
        await db.localExerciseSets.delete(localSet!.localId);
      });
      debugPrint('✅ Local-only set deleted');
    } else {
      // Mark for deletion when online
      await db.writeTxn(() async {
        localSet!.isSynced = false;
        localSet.syncStatus = 'pending_delete';
        localSet.lastModifiedLocal = DateTime.now();
        await db.localExerciseSets.put(localSet);
      });
      debugPrint('✅ Set marked for deletion, will sync when online');
    }

    return true;
  }
}
