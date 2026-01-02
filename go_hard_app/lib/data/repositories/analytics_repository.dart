import '../services/api_service.dart';
import '../models/workout_stats.dart';

class AnalyticsRepository {
  final ApiService _apiService;

  AnalyticsRepository(this._apiService);

  /// Get overall workout statistics
  Future<WorkoutStats> getWorkoutStats() async {
    final data = await _apiService.get<Map<String, dynamic>>('analytics/stats');
    return WorkoutStats.fromJson(data);
  }

  /// Get progress for all exercises
  Future<List<ExerciseProgress>> getExerciseProgress() async {
    final data = await _apiService.get<List<dynamic>>(
      'analytics/exercise-progress',
    );
    return data
        .map((json) => ExerciseProgress.fromJson(json as Map<String, dynamic>))
        .toList();
  }

  /// Get progress over time for specific exercise
  Future<List<ProgressDataPoint>> getExerciseProgressOverTime(
    int exerciseTemplateId, {
    int days = 90,
  }) async {
    final data = await _apiService.get<List<dynamic>>(
      'analytics/exercise-progress/$exerciseTemplateId?days=$days',
    );
    return data
        .map((json) => ProgressDataPoint.fromJson(json as Map<String, dynamic>))
        .toList();
  }

  /// Get muscle group volume distribution
  Future<List<MuscleGroupVolume>> getMuscleGroupVolume({int days = 30}) async {
    final data = await _apiService.get<List<dynamic>>(
      'analytics/muscle-group-volume?days=$days',
    );
    return data
        .map((json) => MuscleGroupVolume.fromJson(json as Map<String, dynamic>))
        .toList();
  }

  /// Get all personal records
  Future<List<PersonalRecord>> getPersonalRecords() async {
    final data = await _apiService.get<List<dynamic>>(
      'analytics/personal-records',
    );
    return data
        .map((json) => PersonalRecord.fromJson(json as Map<String, dynamic>))
        .toList();
  }

  /// Get volume over time
  Future<List<ProgressDataPoint>> getVolumeOverTime({int days = 90}) async {
    final data = await _apiService.get<List<dynamic>>(
      'analytics/volume-over-time?days=$days',
    );
    return data
        .map((json) => ProgressDataPoint.fromJson(json as Map<String, dynamic>))
        .toList();
  }
}
