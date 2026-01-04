import 'dart:async';
import 'package:flutter/foundation.dart';
import '../data/models/exercise_template.dart';
import '../data/repositories/exercise_repository.dart';
import '../core/services/connectivity_service.dart';

/// Provider for exercise library browsing
/// Replaces ExercisesViewModel from MAUI app
class ExercisesProvider extends ChangeNotifier {
  final ExerciseRepository _exerciseRepository;
  final ConnectivityService? _connectivity;

  List<ExerciseTemplate> _exercises = [];
  List<ExerciseTemplate> _filteredExercises = [];
  bool _isLoading = false;
  String? _errorMessage;

  String? _selectedCategory;
  String? _selectedMuscleGroup;

  StreamSubscription<bool>? _connectivitySubscription;

  ExercisesProvider(this._exerciseRepository, [this._connectivity]) {
    // Auto-load exercises on initialization to cache them for offline use
    loadExercises();

    // Listen for connectivity changes and refresh when going online
    _connectivitySubscription = _connectivity?.connectivityStream.listen((
      isOnline,
    ) {
      if (isOnline) {
        debugPrint('📡 Connection restored - refreshing exercise library');
        loadExercises(showLoading: false);
      }
    });
  }

  // Getters
  List<ExerciseTemplate> get exercises => _exercises;
  List<ExerciseTemplate> get filteredExercises => _filteredExercises;
  bool get isLoading => _isLoading;
  String? get errorMessage => _errorMessage;
  String? get selectedCategory => _selectedCategory;
  String? get selectedMuscleGroup => _selectedMuscleGroup;

  /// Load all exercise templates
  Future<void> loadExercises({bool showLoading = true}) async {
    if (_isLoading) return;

    if (showLoading) {
      _isLoading = true;
      _errorMessage = null;
      notifyListeners();
    }

    try {
      _exercises = await _exerciseRepository.getExerciseTemplates();
      _applyFilters();
    } catch (e) {
      _errorMessage =
          'Failed to load exercises: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Load exercises error: $e');
    } finally {
      if (showLoading) {
        _isLoading = false;
      }
      notifyListeners();
    }
  }

  /// Filter by category
  void filterByCategory(String? category) {
    _selectedCategory = category;
    _applyFilters();
  }

  /// Filter by muscle group
  void filterByMuscleGroup(String? muscleGroup) {
    _selectedMuscleGroup = muscleGroup;
    _applyFilters();
  }

  /// Apply current filters
  void _applyFilters() {
    _filteredExercises =
        _exercises.where((exercise) {
          bool matchesCategory =
              _selectedCategory == null ||
              _selectedCategory == 'All' ||
              exercise.category?.toLowerCase() ==
                  _selectedCategory?.toLowerCase();

          bool matchesMuscleGroup =
              _selectedMuscleGroup == null ||
              exercise.muscleGroup?.toLowerCase() ==
                  _selectedMuscleGroup?.toLowerCase();

          return matchesCategory && matchesMuscleGroup;
        }).toList();

    notifyListeners();
  }

  /// Refresh exercises (pull-to-refresh)
  /// Don't show loading indicator for smooth UX
  Future<void> refresh() async {
    await loadExercises(showLoading: false);
  }

  /// Clear filters
  void clearFilters() {
    _selectedCategory = null;
    _selectedMuscleGroup = null;
    _applyFilters();
  }

  /// Clear error message
  void clearError() {
    _errorMessage = null;
    notifyListeners();
  }

  @override
  void dispose() {
    _connectivitySubscription?.cancel();
    super.dispose();
  }
}
