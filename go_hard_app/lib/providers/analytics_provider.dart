import 'package:flutter/foundation.dart';
import '../data/repositories/analytics_repository.dart';
import '../data/models/workout_stats.dart';

class AnalyticsProvider extends ChangeNotifier {
  final AnalyticsRepository _repository;

  WorkoutStats? _workoutStats;
  List<ExerciseProgress> _exerciseProgress = [];
  List<PersonalRecord> _personalRecords = [];
  List<MuscleGroupVolume> _muscleGroupVolume = [];

  bool _isLoading = false;
  String? _errorMessage;

  AnalyticsProvider(this._repository);

  // Getters
  WorkoutStats? get workoutStats => _workoutStats;
  List<ExerciseProgress> get exerciseProgress => _exerciseProgress;
  List<PersonalRecord> get personalRecords => _personalRecords;
  List<MuscleGroupVolume> get muscleGroupVolume => _muscleGroupVolume;
  bool get isLoading => _isLoading;
  String? get errorMessage => _errorMessage;

  /// Load all analytics data
  Future<void> loadAnalytics() async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      // Load all data in parallel
      final results = await Future.wait([
        _repository.getWorkoutStats(),
        _repository.getExerciseProgress(),
        _repository.getPersonalRecords(),
        _repository.getMuscleGroupVolume(days: 30),
      ]);

      _workoutStats = results[0] as WorkoutStats;
      _exerciseProgress = results[1] as List<ExerciseProgress>;
      _personalRecords = results[2] as List<PersonalRecord>;
      _muscleGroupVolume = results[3] as List<MuscleGroupVolume>;
    } catch (e) {
      _errorMessage =
          'Failed to load analytics: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Analytics error: $e');
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  /// Get progress over time for specific exercise
  Future<List<ProgressDataPoint>> getExerciseProgressOverTime(
    int exerciseTemplateId, {
    int days = 90,
  }) async {
    try {
      return await _repository.getExerciseProgressOverTime(
        exerciseTemplateId,
        days: days,
      );
    } catch (e) {
      debugPrint('Error loading exercise progress: $e');
      return [];
    }
  }

  /// Get volume over time
  Future<List<ProgressDataPoint>> getVolumeOverTime({int days = 90}) async {
    try {
      return await _repository.getVolumeOverTime(days: days);
    } catch (e) {
      debugPrint('Error loading volume over time: $e');
      return [];
    }
  }

  /// Refresh analytics data
  Future<void> refresh() async {
    await loadAnalytics();
  }

  void clearError() {
    _errorMessage = null;
    notifyListeners();
  }
}
