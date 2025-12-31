import 'package:flutter/foundation.dart';
import '../data/models/exercise_template.dart';
import '../data/repositories/exercise_repository.dart';

/// Provider for exercise detail view
/// Replaces ExerciseDetailViewModel from MAUI app
class ExerciseDetailProvider extends ChangeNotifier {
  final ExerciseRepository _exerciseRepository;

  ExerciseTemplate? _exercise;
  bool _isLoading = false;
  String? _errorMessage;

  ExerciseDetailProvider(this._exerciseRepository);

  // Getters
  ExerciseTemplate? get exercise => _exercise;
  bool get isLoading => _isLoading;
  String? get errorMessage => _errorMessage;

  /// Load exercise template by ID
  Future<void> loadExercise(int exerciseId) async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      _exercise = await _exerciseRepository.getExerciseTemplate(exerciseId);
    } catch (e) {
      _errorMessage =
          'Failed to load exercise: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Load exercise error: $e');
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  /// Clear error message
  void clearError() {
    _errorMessage = null;
    notifyListeners();
  }
}
