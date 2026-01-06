import 'package:flutter/foundation.dart';
import '../../core/constants/api_config.dart';
import '../../core/services/connectivity_service.dart';
import '../models/goal.dart';
import '../models/goal_progress.dart';
import '../services/api_service.dart';

/// Repository for goals operations with offline caching
class GoalsRepository {
  final ApiService _apiService;
  final ConnectivityService? _connectivity;

  GoalsRepository(this._apiService, [this._connectivity]);

  /// Get all goals for the current user
  /// Optional filter: isActive (true for active goals, false for inactive/completed)
  Future<List<Goal>> getGoals({bool? isActive}) async {
    final isOnline = _connectivity?.isOnline ?? true;

    if (!isOnline) {
      debugPrint('📴 Offline - goals feature requires online connection');
      return [];
    }

    try {
      final queryParams =
          isActive != null ? {'isActive': isActive.toString()} : null;
      final data = await _apiService.get<List<dynamic>>(
        ApiConfig.goals,
        queryParameters: queryParams,
      );

      return data
          .map((json) => Goal.fromJson(json as Map<String, dynamic>))
          .toList();
    } catch (e) {
      debugPrint('⚠️ Failed to fetch goals: $e');
      rethrow;
    }
  }

  /// Get a specific goal by ID with progress history
  Future<Goal> getGoalById(int id) async {
    final data = await _apiService.get<Map<String, dynamic>>(
      ApiConfig.goalById(id),
    );
    return Goal.fromJson(data);
  }

  /// Create a new goal
  Future<Goal> createGoal(Goal goal) async {
    final data = await _apiService.post<Map<String, dynamic>>(
      ApiConfig.goals,
      data: goal.toJson(),
    );
    return Goal.fromJson(data);
  }

  /// Update an existing goal
  Future<void> updateGoal(int id, Goal goal) async {
    await _apiService.put<void>(ApiConfig.goalById(id), data: goal.toJson());
  }

  /// Delete a goal
  Future<void> deleteGoal(int id) async {
    await _apiService.delete(ApiConfig.goalById(id));
  }

  /// Mark a goal as completed
  Future<void> completeGoal(int id) async {
    await _apiService.put<void>(ApiConfig.goalComplete(id));
  }

  /// Add progress entry for a goal
  /// This also updates the goal's current value and may auto-complete the goal
  Future<GoalProgress> addProgress(int goalId, GoalProgress progress) async {
    final data = await _apiService.post<Map<String, dynamic>>(
      ApiConfig.goalProgress(goalId),
      data: progress.toJson(),
    );
    return GoalProgress.fromJson(data);
  }

  /// Get progress history for a goal
  Future<List<GoalProgress>> getProgressHistory(int goalId) async {
    final data = await _apiService.get<List<dynamic>>(
      ApiConfig.goalHistory(goalId),
    );
    return data
        .map((json) => GoalProgress.fromJson(json as Map<String, dynamic>))
        .toList();
  }
}
