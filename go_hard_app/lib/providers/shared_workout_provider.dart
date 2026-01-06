import 'dart:async';
import 'package:flutter/foundation.dart';
import 'package:isar/isar.dart';
import '../data/models/shared_workout.dart';
import '../data/repositories/shared_workout_repository.dart';
import '../core/services/connectivity_service.dart';

/// Provider for community shared workouts management
class SharedWorkoutProvider extends ChangeNotifier {
  final SharedWorkoutRepository _repository;
  final ConnectivityService _connectivity;

  List<SharedWorkout> _sharedWorkouts = [];
  List<SharedWorkout> _savedWorkouts = [];
  List<SharedWorkout> _mySharedWorkouts = [];
  bool _isLoading = false;
  String? _errorMessage;
  StreamSubscription<bool>? _connectivitySubscription;

  // Filter states
  String? _selectedCategory;
  String? _selectedDifficulty;

  SharedWorkoutProvider(this._repository, this._connectivity) {
    // Listen for connectivity changes
    _connectivitySubscription = _connectivity.connectivityStream.listen((
      isOnline,
    ) {
      if (isOnline) {
        debugPrint('📡 Connection restored - refreshing shared workouts');
        loadSharedWorkouts(showLoading: false);
      }
    });
  }

  @override
  void dispose() {
    _connectivitySubscription?.cancel();
    super.dispose();
  }

  // Getters
  List<SharedWorkout> get sharedWorkouts => _sharedWorkouts;
  List<SharedWorkout> get savedWorkouts => _savedWorkouts;
  List<SharedWorkout> get mySharedWorkouts => _mySharedWorkouts;
  bool get isLoading => _isLoading;
  String? get errorMessage => _errorMessage;
  bool get isOnline => _connectivity.isOnline;
  String? get selectedCategory => _selectedCategory;
  String? get selectedDifficulty => _selectedDifficulty;

  /// Load community shared workouts
  Future<void> loadSharedWorkouts({bool showLoading = true}) async {
    if (_isLoading) return;

    if (showLoading) {
      _isLoading = true;
      _errorMessage = null;
      notifyListeners();
    }

    try {
      final workouts = await _repository.getSharedWorkouts(
        category: _selectedCategory,
        difficulty: _selectedDifficulty,
        limit: 50,
      );
      _sharedWorkouts = workouts;
      debugPrint('✅ Loaded ${_sharedWorkouts.length} shared workouts');
    } catch (e) {
      _errorMessage =
          'Failed to load shared workouts: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('❌ Load shared workouts error: $e');
    } finally {
      if (showLoading) {
        _isLoading = false;
      }
      notifyListeners();
    }
  }

  /// Load saved workouts for current user
  Future<void> loadSavedWorkouts() async {
    try {
      final workouts = await _repository.getSavedWorkouts();
      _savedWorkouts = workouts;
      notifyListeners();
      debugPrint('✅ Loaded ${_savedWorkouts.length} saved workouts');
    } catch (e) {
      debugPrint('❌ Load saved workouts error: $e');
    }
  }

  /// Load workouts shared by current user
  Future<void> loadMySharedWorkouts(int userId) async {
    try {
      final workouts = await _repository.getSharedWorkoutsByUser(userId);
      _mySharedWorkouts = workouts;
      notifyListeners();
      debugPrint('✅ Loaded ${_mySharedWorkouts.length} of my shared workouts');
    } catch (e) {
      debugPrint('❌ Load my shared workouts error: $e');
    }
  }

  /// Share a workout to the community
  Future<SharedWorkout?> shareWorkout({
    required int originalId,
    required String type,
    required String workoutName,
    String? description,
    required String exercisesJson,
    required int duration,
    required String category,
    String? difficulty,
  }) async {
    if (!_connectivity.isOnline) {
      _errorMessage = 'Cannot share workout while offline';
      notifyListeners();
      return null;
    }

    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final shared = await _repository.shareWorkout(
        originalId: originalId,
        type: type,
        workoutName: workoutName,
        description: description,
        exercisesJson: exercisesJson,
        duration: duration,
        category: category,
        difficulty: difficulty,
      );

      // Add to my shared workouts list
      _mySharedWorkouts.insert(0, shared);
      _sharedWorkouts.insert(0, shared);

      debugPrint('✅ Successfully shared workout: ${shared.workoutName}');
      _isLoading = false;
      notifyListeners();
      return shared;
    } catch (e) {
      _errorMessage =
          'Failed to share workout: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('❌ Share workout error: $e');
      _isLoading = false;
      notifyListeners();
      return null;
    }
  }

  /// Toggle like on a shared workout
  Future<void> toggleLike(int sharedWorkoutId) async {
    if (!_connectivity.isOnline) {
      _errorMessage = 'Cannot like/unlike while offline';
      notifyListeners();
      return;
    }

    try {
      // Find the workout in our lists
      SharedWorkout? workout = _findWorkoutById(sharedWorkoutId);
      if (workout == null) return;

      final newLikedState = !workout.isLikedByCurrentUser;

      // Optimistically update UI
      workout.isLikedByCurrentUser = newLikedState;
      workout.likeCount += newLikedState ? 1 : -1;
      notifyListeners();

      // Make API call
      await _repository.toggleLike(sharedWorkoutId, newLikedState);
      debugPrint('✅ Toggled like on workout $sharedWorkoutId');
    } catch (e) {
      // Revert on error
      SharedWorkout? workout = _findWorkoutById(sharedWorkoutId);
      if (workout != null) {
        workout.isLikedByCurrentUser = !workout.isLikedByCurrentUser;
        workout.likeCount += workout.isLikedByCurrentUser ? 1 : -1;
        notifyListeners();
      }

      _errorMessage =
          'Failed to like workout: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('❌ Toggle like error: $e');
      notifyListeners();
    }
  }

  /// Toggle save on a shared workout
  Future<void> toggleSave(int sharedWorkoutId) async {
    if (!_connectivity.isOnline) {
      _errorMessage = 'Cannot save/unsave while offline';
      notifyListeners();
      return;
    }

    try {
      // Find the workout in our lists
      SharedWorkout? workout = _findWorkoutById(sharedWorkoutId);
      if (workout == null) return;

      final newSavedState = !workout.isSavedByCurrentUser;

      // Optimistically update UI
      workout.isSavedByCurrentUser = newSavedState;
      workout.saveCount += newSavedState ? 1 : -1;

      // Update saved workouts list
      if (newSavedState) {
        if (!_savedWorkouts.any((w) => w.id == workout.id)) {
          _savedWorkouts.insert(0, workout);
        }
      } else {
        _savedWorkouts.removeWhere((w) => w.id == workout.id);
      }
      notifyListeners();

      // Make API call
      await _repository.toggleSave(sharedWorkoutId, newSavedState);
      debugPrint('✅ Toggled save on workout $sharedWorkoutId');
    } catch (e) {
      // Revert on error
      SharedWorkout? workout = _findWorkoutById(sharedWorkoutId);
      if (workout != null) {
        workout.isSavedByCurrentUser = !workout.isSavedByCurrentUser;
        workout.saveCount += workout.isSavedByCurrentUser ? 1 : -1;

        // Revert saved workouts list
        if (workout.isSavedByCurrentUser) {
          if (!_savedWorkouts.any((w) => w.id == workout.id)) {
            _savedWorkouts.insert(0, workout);
          }
        } else {
          _savedWorkouts.removeWhere((w) => w.id == workout.id);
        }
        notifyListeners();
      }

      _errorMessage =
          'Failed to save workout: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('❌ Toggle save error: $e');
      notifyListeners();
    }
  }

  /// Delete a shared workout (only if created by current user)
  Future<bool> deleteSharedWorkout(int sharedWorkoutId) async {
    if (!_connectivity.isOnline) {
      _errorMessage = 'Cannot delete while offline';
      notifyListeners();
      return false;
    }

    try {
      await _repository.deleteSharedWorkout(sharedWorkoutId);

      // Remove from all lists
      _sharedWorkouts.removeWhere((w) => w.id == sharedWorkoutId);
      _mySharedWorkouts.removeWhere((w) => w.id == sharedWorkoutId);
      _savedWorkouts.removeWhere((w) => w.id == sharedWorkoutId);

      notifyListeners();
      debugPrint('✅ Deleted shared workout $sharedWorkoutId');
      return true;
    } catch (e) {
      _errorMessage =
          'Failed to delete workout: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('❌ Delete shared workout error: $e');
      notifyListeners();
      return false;
    }
  }

  /// Set category filter
  void setCategory(String? category) {
    if (_selectedCategory != category) {
      _selectedCategory = category;
      loadSharedWorkouts();
    }
  }

  /// Set difficulty filter
  void setDifficulty(String? difficulty) {
    if (_selectedDifficulty != difficulty) {
      _selectedDifficulty = difficulty;
      loadSharedWorkouts();
    }
  }

  /// Clear all filters
  void clearFilters() {
    if (_selectedCategory != null || _selectedDifficulty != null) {
      _selectedCategory = null;
      _selectedDifficulty = null;
      loadSharedWorkouts();
    }
  }

  /// Refresh all data
  Future<void> refresh() async {
    await loadSharedWorkouts();
    await loadSavedWorkouts();
  }

  /// Clear error message
  void clearError() {
    _errorMessage = null;
    notifyListeners();
  }

  // === PRIVATE HELPERS ===

  /// Find a workout by ID across all lists
  SharedWorkout? _findWorkoutById(int id) {
    // Try shared workouts first
    var workout = _sharedWorkouts.firstWhere(
      (w) => w.id == id,
      orElse:
          () => _savedWorkouts.firstWhere(
            (w) => w.id == id,
            orElse:
                () => _mySharedWorkouts.firstWhere(
                  (w) => w.id == id,
                  orElse:
                      () => SharedWorkout(
                        originalId: 0,
                        type: '',
                        sharedByUserId: 0,
                        sharedByUserName: '',
                        workoutName: '',
                        exercisesJson: '',
                        duration: 0,
                        category: '',
                        sharedAt: DateTime.now(),
                      ),
                ),
          ),
    );

    return workout.id == Isar.autoIncrement ? null : workout;
  }
}
