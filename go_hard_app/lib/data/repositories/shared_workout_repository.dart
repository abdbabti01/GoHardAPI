import 'package:flutter/foundation.dart';
import 'package:isar/isar.dart';
import '../../core/constants/api_config.dart';
import '../../core/services/connectivity_service.dart';
import '../models/shared_workout.dart';
import '../services/api_service.dart';
import '../services/auth_service.dart';
import '../local/services/local_database_service.dart';

/// Repository for community shared workouts with offline caching
class SharedWorkoutRepository {
  final ApiService _apiService;
  final LocalDatabaseService _localDb;
  final ConnectivityService _connectivity;
  final AuthService _authService;

  SharedWorkoutRepository(
    this._apiService,
    this._localDb,
    this._connectivity,
    this._authService,
  );

  /// Get all shared workouts from community
  /// Offline-first: returns cached data immediately, syncs in background
  Future<List<SharedWorkout>> getSharedWorkouts({
    String? category,
    String? difficulty,
    int? limit = 50,
  }) async {
    final Isar db = _localDb.database;

    // Load from cache first for instant response
    final cachedWorkouts = await _getLocalSharedWorkouts(
      db,
      category: category,
      difficulty: difficulty,
      limit: limit,
    );

    // Sync with server in background if online
    if (_connectivity.isOnline) {
      _syncSharedWorkoutsFromServer(
        db,
        category: category,
        difficulty: difficulty,
        limit: limit,
      ).catchError((e) {
        debugPrint('⚠️ Background sync failed: $e');
      });
    }

    return cachedWorkouts;
  }

  /// Get shared workouts created by a specific user
  Future<List<SharedWorkout>> getSharedWorkoutsByUser(int userId) async {
    if (!_connectivity.isOnline) {
      // Offline: return from cache
      final Isar db = _localDb.database;
      final workouts =
          await db.sharedWorkouts
              .filter()
              .sharedByUserIdEqualTo(userId)
              .sortBySharedAtDesc()
              .findAll();
      return workouts;
    }

    try {
      final data = await _apiService.get<List<dynamic>>(
        '${ApiConfig.sharedWorkouts}/user/$userId',
      );
      return data
          .map(
            (json) => SharedWorkoutJson.fromJson(json as Map<String, dynamic>),
          )
          .toList();
    } catch (e) {
      debugPrint('Error fetching user shared workouts: $e');
      rethrow;
    }
  }

  /// Share a workout to the community
  Future<SharedWorkout> shareWorkout({
    required int originalId,
    required String type, // 'session' or 'template'
    required String workoutName,
    String? description,
    required String exercisesJson,
    required int duration,
    required String category,
    String? difficulty,
  }) async {
    final currentUserId = await _authService.getUserId();
    final userName = await _authService.getUserName() ?? 'Unknown';

    if (currentUserId == null) {
      throw Exception('User not authenticated');
    }

    final sharedWorkout = SharedWorkout(
      originalId: originalId,
      type: type,
      sharedByUserId: currentUserId,
      sharedByUserName: userName,
      workoutName: workoutName,
      description: description,
      exercisesJson: exercisesJson,
      duration: duration,
      category: category,
      difficulty: difficulty,
      sharedAt: DateTime.now(),
    );

    if (!_connectivity.isOnline) {
      throw Exception('Cannot share workout while offline');
    }

    try {
      final data = await _apiService.post<Map<String, dynamic>>(
        ApiConfig.sharedWorkouts,
        data: sharedWorkout.toJson(),
      );
      final created = SharedWorkoutJson.fromJson(data);

      // Cache locally
      final Isar db = _localDb.database;
      await db.writeTxn(() async {
        await db.sharedWorkouts.put(created);
      });

      return created;
    } catch (e) {
      debugPrint('Error sharing workout: $e');
      rethrow;
    }
  }

  /// Toggle like on a shared workout
  Future<void> toggleLike(int sharedWorkoutId, bool isLiked) async {
    if (!_connectivity.isOnline) {
      throw Exception('Cannot like/unlike while offline');
    }

    try {
      await _apiService.post(
        '${ApiConfig.sharedWorkouts}/$sharedWorkoutId/${isLiked ? 'like' : 'unlike'}',
        data: {},
      );

      // Update cache
      final Isar db = _localDb.database;
      final workout = await db.sharedWorkouts.get(sharedWorkoutId);
      if (workout != null) {
        await db.writeTxn(() async {
          workout.isLikedByCurrentUser = isLiked;
          workout.likeCount += isLiked ? 1 : -1;
          await db.sharedWorkouts.put(workout);
        });
      }
    } catch (e) {
      debugPrint('Error toggling like: $e');
      rethrow;
    }
  }

  /// Toggle save on a shared workout
  Future<void> toggleSave(int sharedWorkoutId, bool isSaved) async {
    if (!_connectivity.isOnline) {
      throw Exception('Cannot save/unsave while offline');
    }

    try {
      await _apiService.post(
        '${ApiConfig.sharedWorkouts}/$sharedWorkoutId/${isSaved ? 'save' : 'unsave'}',
        data: {},
      );

      // Update cache
      final Isar db = _localDb.database;
      final workout = await db.sharedWorkouts.get(sharedWorkoutId);
      if (workout != null) {
        await db.writeTxn(() async {
          workout.isSavedByCurrentUser = isSaved;
          workout.saveCount += isSaved ? 1 : -1;
          await db.sharedWorkouts.put(workout);
        });
      }
    } catch (e) {
      debugPrint('Error toggling save: $e');
      rethrow;
    }
  }

  /// Get saved workouts for current user
  Future<List<SharedWorkout>> getSavedWorkouts() async {
    final Isar db = _localDb.database;

    if (!_connectivity.isOnline) {
      // Offline: return from cache
      return await db.sharedWorkouts
          .filter()
          .isSavedByCurrentUserEqualTo(true)
          .sortBySharedAtDesc()
          .findAll();
    }

    try {
      final data = await _apiService.get<List<dynamic>>(
        '${ApiConfig.sharedWorkouts}/saved',
      );
      final workouts =
          data
              .map(
                (json) =>
                    SharedWorkoutJson.fromJson(json as Map<String, dynamic>),
              )
              .toList();

      // Update cache
      await db.writeTxn(() async {
        for (final workout in workouts) {
          await db.sharedWorkouts.put(workout);
        }
      });

      return workouts;
    } catch (e) {
      debugPrint('Error fetching saved workouts: $e');
      rethrow;
    }
  }

  /// Delete a shared workout (only if created by current user)
  Future<void> deleteSharedWorkout(int sharedWorkoutId) async {
    if (!_connectivity.isOnline) {
      throw Exception('Cannot delete while offline');
    }

    try {
      await _apiService.delete('${ApiConfig.sharedWorkouts}/$sharedWorkoutId');

      // Remove from cache
      final Isar db = _localDb.database;
      await db.writeTxn(() async {
        await db.sharedWorkouts.delete(sharedWorkoutId);
      });
    } catch (e) {
      debugPrint('Error deleting shared workout: $e');
      rethrow;
    }
  }

  // === PRIVATE HELPERS ===

  /// Get shared workouts from local cache
  Future<List<SharedWorkout>> _getLocalSharedWorkouts(
    Isar db, {
    String? category,
    String? difficulty,
    int? limit,
  }) async {
    // Build query with filters
    List<SharedWorkout> results;

    if (category != null && difficulty != null) {
      results =
          await db.sharedWorkouts
              .filter()
              .categoryEqualTo(category)
              .and()
              .difficultyEqualTo(difficulty)
              .findAll();
    } else if (category != null) {
      results =
          await db.sharedWorkouts.filter().categoryEqualTo(category).findAll();
    } else if (difficulty != null) {
      results =
          await db.sharedWorkouts
              .filter()
              .difficultyEqualTo(difficulty)
              .findAll();
    } else {
      results = await db.sharedWorkouts.where().findAll();
    }

    // Sort by sharedAt descending
    results.sort((a, b) => b.sharedAt.compareTo(a.sharedAt));

    if (limit != null && results.length > limit) {
      return results.sublist(0, limit);
    }

    return results;
  }

  /// Sync shared workouts from server to local cache
  Future<void> _syncSharedWorkoutsFromServer(
    Isar db, {
    String? category,
    String? difficulty,
    int? limit,
  }) async {
    try {
      var endpoint = ApiConfig.sharedWorkouts;
      final queryParams = <String>[];

      if (category != null) queryParams.add('category=$category');
      if (difficulty != null) queryParams.add('difficulty=$difficulty');
      if (limit != null) queryParams.add('limit=$limit');

      if (queryParams.isNotEmpty) {
        endpoint += '?${queryParams.join('&')}';
      }

      final data = await _apiService.get<List<dynamic>>(endpoint);
      final workouts =
          data
              .map(
                (json) =>
                    SharedWorkoutJson.fromJson(json as Map<String, dynamic>),
              )
              .toList();

      // Update cache
      await db.writeTxn(() async {
        for (final workout in workouts) {
          await db.sharedWorkouts.put(workout);
        }
      });

      debugPrint('✅ Synced ${workouts.length} shared workouts from server');
    } catch (e) {
      debugPrint('⚠️ Failed to sync shared workouts: $e');
      rethrow;
    }
  }
}

// Extension for SharedWorkout JSON serialization
extension SharedWorkoutJson on SharedWorkout {
  Map<String, dynamic> toJson() {
    return {
      'originalId': originalId,
      'type': type,
      'sharedByUserId': sharedByUserId,
      'sharedByUserName': sharedByUserName,
      'workoutName': workoutName,
      'description': description,
      'exercisesJson': exercisesJson,
      'duration': duration,
      'category': category,
      'difficulty': difficulty,
      'sharedAt': sharedAt.toIso8601String(),
    };
  }

  static SharedWorkout fromJson(Map<String, dynamic> json) {
    return SharedWorkout(
      id: json['id'] as int? ?? Isar.autoIncrement,
      originalId: json['originalId'] as int,
      type: json['type'] as String,
      sharedByUserId: json['sharedByUserId'] as int,
      sharedByUserName: json['sharedByUserName'] as String,
      workoutName: json['workoutName'] as String,
      description: json['description'] as String?,
      exercisesJson: json['exercisesJson'] as String,
      duration: json['duration'] as int,
      category: json['category'] as String,
      difficulty: json['difficulty'] as String?,
      likeCount: json['likeCount'] as int? ?? 0,
      saveCount: json['saveCount'] as int? ?? 0,
      commentCount: json['commentCount'] as int? ?? 0,
      isLikedByCurrentUser: json['isLikedByCurrentUser'] as bool? ?? false,
      isSavedByCurrentUser: json['isSavedByCurrentUser'] as bool? ?? false,
      sharedAt: DateTime.parse(json['sharedAt'] as String),
    );
  }
}
