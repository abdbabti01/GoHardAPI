import '../../core/constants/api_config.dart';
import '../models/exercise_template.dart';
import '../models/exercise_set.dart';
import '../services/api_service.dart';

/// Repository for exercise and exercise template operations
class ExerciseRepository {
  final ApiService _apiService;

  ExerciseRepository(this._apiService);

  // Exercise Templates

  /// Get all exercise templates with optional filtering
  Future<List<ExerciseTemplate>> getExerciseTemplates({
    String? category,
    String? muscleGroup,
    bool? isCustom,
  }) async {
    final queryParams = <String, dynamic>{};
    if (category != null) queryParams['category'] = category;
    if (muscleGroup != null) queryParams['muscleGroup'] = muscleGroup;
    if (isCustom != null) queryParams['isCustom'] = isCustom;

    final data = await _apiService.get<List<dynamic>>(
      ApiConfig.exerciseTemplates,
      queryParameters: queryParams.isNotEmpty ? queryParams : null,
    );
    return data
        .map((json) => ExerciseTemplate.fromJson(json as Map<String, dynamic>))
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
  Future<List<String>> getCategories() async {
    final data = await _apiService.get<List<dynamic>>(
      ApiConfig.exerciseTemplateCategories,
    );
    return data.map((e) => e.toString()).toList();
  }

  /// Get all available muscle groups
  Future<List<String>> getMuscleGroups() async {
    final data = await _apiService.get<List<dynamic>>(
      ApiConfig.exerciseTemplateMuscleGroups,
    );
    return data.map((e) => e.toString()).toList();
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
    return ExerciseSet.fromJson(data);
  }

  /// Delete exercise set
  Future<bool> deleteExerciseSet(int id) async {
    return await _apiService.delete(ApiConfig.exerciseSetById(id));
  }
}
