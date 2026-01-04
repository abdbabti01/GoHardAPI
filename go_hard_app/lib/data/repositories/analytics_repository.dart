import 'package:flutter/foundation.dart';
import 'package:isar/isar.dart';
import '../services/api_service.dart';
import '../models/workout_stats.dart';
import '../local/services/local_database_service.dart';
import '../local/models/local_session.dart';
import '../local/models/local_exercise.dart';
import '../local/models/local_exercise_set.dart';
import '../../core/services/connectivity_service.dart';
import '../services/auth_service.dart';

class AnalyticsRepository {
  final ApiService _apiService;
  final LocalDatabaseService _localDb;
  final ConnectivityService _connectivity;
  final AuthService _authService;

  AnalyticsRepository(
    this._apiService,
    this._localDb,
    this._connectivity,
    this._authService,
  );

  /// Get overall workout statistics
  /// Offline-first: calculates from local DB when offline
  Future<WorkoutStats> getWorkoutStats() async {
    if (_connectivity.isOnline) {
      try {
        final data = await _apiService.get<Map<String, dynamic>>(
          'analytics/stats',
        );
        return WorkoutStats.fromJson(data);
      } catch (e) {
        debugPrint('⚠️ API failed, falling back to local calculation: $e');
        return await _calculateWorkoutStatsFromLocal();
      }
    } else {
      debugPrint('📴 Offline - calculating stats from local database');
      return await _calculateWorkoutStatsFromLocal();
    }
  }

  /// Calculate workout stats from local database
  Future<WorkoutStats> _calculateWorkoutStatsFromLocal() async {
    final db = _localDb.database;
    final userId = await _authService.getUserId();

    if (userId == null) {
      throw Exception('No authenticated user');
    }

    // Get all completed sessions for current user
    final sessions =
        await db.localSessions
            .filter()
            .userIdEqualTo(userId)
            .statusEqualTo('completed')
            .findAll();

    // Sort by date descending
    sessions.sort((a, b) => b.date.compareTo(a.date));

    // Calculate basic stats
    final totalWorkouts = sessions.length;
    int totalDuration = 0;
    int totalSets = 0;
    int totalReps = 0;
    double totalVolume = 0;

    // Calculate weekly/monthly counts
    final now = DateTime.now();
    final thisWeekStart = now.subtract(Duration(days: now.weekday - 1));
    final thisMonthStart = DateTime(now.year, now.month, 1);
    int workoutsThisWeek = 0;
    int workoutsThisMonth = 0;

    for (final session in sessions) {
      // Duration
      if (session.duration != null) {
        totalDuration += session.duration!;
      }

      // Weekly/monthly counts
      if (session.date.isAfter(thisWeekStart)) workoutsThisWeek++;
      if (session.date.isAfter(thisMonthStart)) workoutsThisMonth++;

      // Sets, reps, and volume
      final exercises =
          await db.localExercises
              .filter()
              .sessionLocalIdEqualTo(session.localId)
              .findAll();

      for (final exercise in exercises) {
        final sets =
            await db.localExerciseSets
                .filter()
                .exerciseLocalIdEqualTo(exercise.localId)
                .findAll();

        totalSets += sets.length;
        for (final set in sets) {
          if (set.reps != null) totalReps += set.reps!;
          if (set.weight != null && set.reps != null) {
            totalVolume += set.weight! * set.reps!;
          }
        }
      }
    }

    // Calculate streaks
    int currentStreak = 0;
    int longestStreak = 0;
    if (sessions.isNotEmpty) {
      final today = DateTime(now.year, now.month, now.day);
      var checkDate = today;
      int streak = 0;

      for (final session in sessions) {
        final sessionDate = DateTime(
          session.date.year,
          session.date.month,
          session.date.day,
        );

        if (sessionDate == checkDate) {
          streak++;
          if (streak > longestStreak) longestStreak = streak;
          if (currentStreak == 0 || checkDate == today) currentStreak = streak;
          checkDate = checkDate.subtract(const Duration(days: 1));
        } else if (sessionDate.isBefore(checkDate)) {
          // Gap in streak
          if (streak > longestStreak) longestStreak = streak;
          streak = 0;
          checkDate = sessionDate;
        }
      }
    }

    final averageDuration =
        totalWorkouts > 0 ? (totalDuration / totalWorkouts).round() : 0;

    return WorkoutStats(
      totalWorkouts: totalWorkouts,
      totalDuration: totalDuration,
      averageDuration: averageDuration,
      currentStreak: currentStreak,
      longestStreak: longestStreak,
      workoutsThisWeek: workoutsThisWeek,
      workoutsThisMonth: workoutsThisMonth,
      totalSets: totalSets,
      totalReps: totalReps,
      totalVolume: totalVolume,
      firstWorkoutDate: sessions.isNotEmpty ? sessions.last.date : null,
      lastWorkoutDate: sessions.isNotEmpty ? sessions.first.date : null,
    );
  }

  /// Get progress for all exercises
  /// Returns empty list when offline (online-only feature)
  Future<List<ExerciseProgress>> getExerciseProgress() async {
    if (!_connectivity.isOnline) {
      debugPrint('📴 Offline - exercise progress unavailable');
      return [];
    }

    try {
      final data = await _apiService.get<List<dynamic>>(
        'analytics/exercise-progress',
      );
      return data
          .map(
            (json) => ExerciseProgress.fromJson(json as Map<String, dynamic>),
          )
          .toList();
    } catch (e) {
      debugPrint('⚠️ Failed to load exercise progress: $e');
      return [];
    }
  }

  /// Get progress over time for specific exercise
  /// Returns empty list when offline (online-only feature)
  Future<List<ProgressDataPoint>> getExerciseProgressOverTime(
    int exerciseTemplateId, {
    int days = 90,
  }) async {
    if (!_connectivity.isOnline) {
      debugPrint('📴 Offline - exercise progress over time unavailable');
      return [];
    }

    try {
      final data = await _apiService.get<List<dynamic>>(
        'analytics/exercise-progress/$exerciseTemplateId?days=$days',
      );
      return data
          .map(
            (json) => ProgressDataPoint.fromJson(json as Map<String, dynamic>),
          )
          .toList();
    } catch (e) {
      debugPrint('⚠️ Failed to load exercise progress over time: $e');
      return [];
    }
  }

  /// Get muscle group volume distribution
  /// Returns empty list when offline (online-only feature)
  Future<List<MuscleGroupVolume>> getMuscleGroupVolume({int days = 30}) async {
    if (!_connectivity.isOnline) {
      debugPrint('📴 Offline - muscle group volume unavailable');
      return [];
    }

    try {
      final data = await _apiService.get<List<dynamic>>(
        'analytics/muscle-group-volume?days=$days',
      );
      return data
          .map(
            (json) => MuscleGroupVolume.fromJson(json as Map<String, dynamic>),
          )
          .toList();
    } catch (e) {
      debugPrint('⚠️ Failed to load muscle group volume: $e');
      return [];
    }
  }

  /// Get all personal records
  /// Returns empty list when offline (online-only feature)
  Future<List<PersonalRecord>> getPersonalRecords() async {
    if (!_connectivity.isOnline) {
      debugPrint('📴 Offline - personal records unavailable');
      return [];
    }

    try {
      final data = await _apiService.get<List<dynamic>>(
        'analytics/personal-records',
      );
      return data
          .map((json) => PersonalRecord.fromJson(json as Map<String, dynamic>))
          .toList();
    } catch (e) {
      debugPrint('⚠️ Failed to load personal records: $e');
      return [];
    }
  }

  /// Get volume over time
  /// Returns empty list when offline (online-only feature)
  Future<List<ProgressDataPoint>> getVolumeOverTime({int days = 90}) async {
    if (!_connectivity.isOnline) {
      debugPrint('📴 Offline - volume over time unavailable');
      return [];
    }

    try {
      final data = await _apiService.get<List<dynamic>>(
        'analytics/volume-over-time?days=$days',
      );
      return data
          .map(
            (json) => ProgressDataPoint.fromJson(json as Map<String, dynamic>),
          )
          .toList();
    } catch (e) {
      debugPrint('⚠️ Failed to load volume over time: $e');
      return [];
    }
  }
}
