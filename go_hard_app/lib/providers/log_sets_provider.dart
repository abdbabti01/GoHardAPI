import 'package:flutter/foundation.dart';
import '../data/models/exercise_set.dart';
import '../data/repositories/exercise_repository.dart';

/// Provider for logging exercise sets
/// Replaces LogSetsViewModel from MAUI app
class LogSetsProvider extends ChangeNotifier {
  final ExerciseRepository _exerciseRepository;

  List<ExerciseSet> _sets = [];
  bool _isLoading = false;
  String? _errorMessage;

  LogSetsProvider(this._exerciseRepository);

  // Getters
  List<ExerciseSet> get sets => _sets;
  bool get isLoading => _isLoading;
  String? get errorMessage => _errorMessage;

  /// Load sets for an exercise
  Future<void> loadSets(int exerciseId) async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      _sets = await _exerciseRepository.getExerciseSets(exerciseId);
      // Sort by set number
      _sets.sort((a, b) => a.setNumber.compareTo(b.setNumber));
    } catch (e) {
      _errorMessage =
          'Failed to load sets: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Load sets error: $e');
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  /// Add a new set
  Future<bool> addSet({
    required int exerciseId,
    required int reps,
    required double weight,
  }) async {
    try {
      // Calculate next set number
      final setNumber =
          _sets.isEmpty
              ? 1
              : _sets.map((s) => s.setNumber).reduce((a, b) => a > b ? a : b) +
                  1;

      final newSet = ExerciseSet(
        id: 0, // Will be assigned by server
        exerciseId: exerciseId,
        setNumber: setNumber,
        reps: reps,
        weight: weight,
        isCompleted: false,
      );

      final createdSet = await _exerciseRepository.createExerciseSet(newSet);
      _sets.add(createdSet);
      _sets.sort((a, b) => a.setNumber.compareTo(b.setNumber));
      notifyListeners();
      return true;
    } catch (e) {
      _errorMessage =
          'Failed to add set: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Add set error: $e');
      notifyListeners();
      return false;
    }
  }

  /// Mark set as complete
  Future<void> completeSet(ExerciseSet set) async {
    try {
      final updatedSet = await _exerciseRepository.completeExerciseSet(set.id);

      // Update in list
      final index = _sets.indexWhere((s) => s.id == set.id);
      if (index != -1) {
        _sets[index] = updatedSet;
        notifyListeners();
      }
    } catch (e) {
      _errorMessage =
          'Failed to complete set: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Complete set error: $e');
      notifyListeners();
    }
  }

  /// Delete a set
  Future<bool> deleteSet(ExerciseSet set) async {
    try {
      final success = await _exerciseRepository.deleteExerciseSet(set.id);
      if (success) {
        _sets.remove(set);

        // Renumber remaining sets
        for (int i = 0; i < _sets.length; i++) {
          _sets[i] = ExerciseSet(
            id: _sets[i].id,
            exerciseId: _sets[i].exerciseId,
            setNumber: i + 1,
            reps: _sets[i].reps,
            weight: _sets[i].weight,
            duration: _sets[i].duration,
            isCompleted: _sets[i].isCompleted,
            completedAt: _sets[i].completedAt,
            notes: _sets[i].notes,
          );
        }

        notifyListeners();
        return true;
      }
      return false;
    } catch (e) {
      _errorMessage =
          'Failed to delete set: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Delete set error: $e');
      notifyListeners();
      return false;
    }
  }

  /// Clear error message
  void clearError() {
    _errorMessage = null;
    notifyListeners();
  }
}
