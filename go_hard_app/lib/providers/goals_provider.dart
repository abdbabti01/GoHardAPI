import 'dart:async';
import 'package:flutter/material.dart';
import '../data/models/goal.dart';
import '../data/models/goal_progress.dart';
import '../data/repositories/goals_repository.dart';
import '../core/services/connectivity_service.dart';

/// Provider for goals management
class GoalsProvider extends ChangeNotifier {
  final GoalsRepository _goalsRepository;
  final ConnectivityService? _connectivity;

  List<Goal> _goals = [];
  List<Goal> _activeGoals = [];
  List<Goal> _completedGoals = [];
  bool _isLoading = false;
  bool _isCreating = false;
  bool _isUpdating = false;
  String? _errorMessage;

  StreamSubscription<bool>? _connectivitySubscription;

  GoalsProvider(this._goalsRepository, [this._connectivity]) {
    // Listen for connectivity changes and refresh when going online
    _connectivitySubscription = _connectivity?.connectivityStream.listen((
      isOnline,
    ) {
      if (isOnline && _goals.isEmpty) {
        debugPrint('📡 Connection restored - loading goals');
        loadGoals();
      }
    });
  }

  // Getters
  List<Goal> get goals => _goals;
  List<Goal> get activeGoals => _activeGoals;
  List<Goal> get completedGoals => _completedGoals;
  bool get isLoading => _isLoading;
  bool get isCreating => _isCreating;
  bool get isUpdating => _isUpdating;
  String? get errorMessage => _errorMessage;

  /// Load all goals for the current user
  Future<void> loadGoals({bool? isActive}) async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      _goals = await _goalsRepository.getGoals(isActive: isActive);

      // Split into active and completed
      _activeGoals = _goals.where((g) => g.isActive).toList();
      _completedGoals = _goals.where((g) => g.isCompleted).toList();

      debugPrint(
        '✅ Loaded ${_goals.length} goals (${_activeGoals.length} active, ${_completedGoals.length} completed)',
      );
    } catch (e) {
      _errorMessage =
          'Failed to load goals: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Load goals error: $e');
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  /// Get a specific goal by ID with progress history
  Future<Goal?> getGoalById(int id) async {
    try {
      return await _goalsRepository.getGoalById(id);
    } catch (e) {
      _errorMessage =
          'Failed to load goal: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Load goal error: $e');
      notifyListeners();
      return null;
    }
  }

  /// Create a new goal
  Future<bool> createGoal(Goal goal) async {
    _isCreating = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final newGoal = await _goalsRepository.createGoal(goal);
      _goals.add(newGoal);
      _activeGoals.add(newGoal);

      debugPrint('✅ Created goal: ${newGoal.goalType}');
      _isCreating = false;
      notifyListeners();
      return true;
    } catch (e) {
      _errorMessage =
          'Failed to create goal: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Create goal error: $e');
      _isCreating = false;
      notifyListeners();
      return false;
    }
  }

  /// Update an existing goal
  Future<bool> updateGoal(int id, Goal goal) async {
    _isUpdating = true;
    _errorMessage = null;
    notifyListeners();

    try {
      await _goalsRepository.updateGoal(id, goal);

      // Update local list
      final index = _goals.indexWhere((g) => g.id == id);
      if (index != -1) {
        _goals[index] = goal;
        _activeGoals = _goals.where((g) => g.isActive).toList();
        _completedGoals = _goals.where((g) => g.isCompleted).toList();
      }

      debugPrint('✅ Updated goal: $id');
      _isUpdating = false;
      notifyListeners();
      return true;
    } catch (e) {
      _errorMessage =
          'Failed to update goal: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Update goal error: $e');
      _isUpdating = false;
      notifyListeners();
      return false;
    }
  }

  /// Delete a goal
  Future<bool> deleteGoal(int id) async {
    _errorMessage = null;
    notifyListeners();

    try {
      await _goalsRepository.deleteGoal(id);

      _goals.removeWhere((g) => g.id == id);
      _activeGoals.removeWhere((g) => g.id == id);
      _completedGoals.removeWhere((g) => g.id == id);

      debugPrint('✅ Deleted goal: $id');
      notifyListeners();
      return true;
    } catch (e) {
      _errorMessage =
          'Failed to delete goal: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Delete goal error: $e');
      notifyListeners();
      return false;
    }
  }

  /// Mark a goal as completed
  Future<bool> completeGoal(int id) async {
    _errorMessage = null;
    notifyListeners();

    try {
      await _goalsRepository.completeGoal(id);

      // Reload to get updated goal
      await loadGoals();

      debugPrint('✅ Completed goal: $id');
      return true;
    } catch (e) {
      _errorMessage =
          'Failed to complete goal: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Complete goal error: $e');
      notifyListeners();
      return false;
    }
  }

  /// Add progress entry for a goal
  Future<bool> addProgress(int goalId, GoalProgress progress) async {
    _errorMessage = null;
    notifyListeners();

    try {
      await _goalsRepository.addProgress(goalId, progress);

      // Reload to get updated goal with new progress
      await loadGoals();

      debugPrint('✅ Added progress to goal: $goalId');
      return true;
    } catch (e) {
      _errorMessage =
          'Failed to add progress: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Add progress error: $e');
      notifyListeners();
      return false;
    }
  }

  /// Get progress history for a goal
  Future<List<GoalProgress>> getProgressHistory(int goalId) async {
    try {
      return await _goalsRepository.getProgressHistory(goalId);
    } catch (e) {
      _errorMessage =
          'Failed to load progress history: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Load progress history error: $e');
      notifyListeners();
      return [];
    }
  }

  /// Clear error message
  void clearError() {
    _errorMessage = null;
    notifyListeners();
  }

  /// Clear all goals data (called on logout)
  void clear() {
    _goals = [];
    _activeGoals = [];
    _completedGoals = [];
    _errorMessage = null;
    _isLoading = false;
    _isCreating = false;
    _isUpdating = false;
    notifyListeners();
    debugPrint('🧹 GoalsProvider cleared');
  }

  @override
  void dispose() {
    _connectivitySubscription?.cancel();
    super.dispose();
  }
}
