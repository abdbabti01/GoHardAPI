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
                .map((json) =>
                    ExerciseTemplate.fromJson(json as Map<String, dynamic>))
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
  Future<List<ExerciseSet>> getExerciseSets(int exerciseId) async {
    final data = await _apiService.get<List<dynamic>>(
      ApiConfig.exerciseSetsByExerciseId(exerciseId),
    );
    return data
        .map((json) => ExerciseSet.fromJson(json as Map<String, dynamic>))
        .toList();
  }

  /// Create new exercise set
  Future<ExerciseSet> createExerciseSet(ExerciseSet exerciseSet) async {
    final data = await _apiService.post<Map<String, dynamic>>(
      ApiConfig.exerciseSets,
      data: exerciseSet.toJson(),
    );
    return ExerciseSet.fromJson(data);
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
  Future<ExerciseSet> completeExerciseSet(int id) async {
    final data = await _apiService.patch<Map<String, dynamic>>(
      ApiConfig.exerciseSetComplete(id),
      data: {},
    );
    if (data == null) {
      throw Exception('Failed to complete exercise set: No data returned');
    }
    return ExerciseSet.fromJson(data);
  }

  /// Delete exercise set
  Future<bool> deleteExerciseSet(int id) async {
    return await _apiService.delete(ApiConfig.exerciseSetById(id));
  }
}
