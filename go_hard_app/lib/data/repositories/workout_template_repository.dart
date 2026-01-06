import 'package:flutter/foundation.dart';
import 'package:isar/isar.dart';
import '../../core/constants/api_config.dart';
import '../../core/services/connectivity_service.dart';
import '../models/workout_template.dart';
import '../services/api_service.dart';
import '../services/auth_service.dart';
import '../local/services/local_database_service.dart';

/// Repository for workout templates with offline support
class WorkoutTemplateRepository {
  final ApiService _apiService;
  final LocalDatabaseService _localDb;
  final ConnectivityService _connectivity;
  final AuthService _authService;

  WorkoutTemplateRepository(
    this._apiService,
    this._localDb,
    this._connectivity,
    this._authService,
  );

  /// Get all templates for current user
  /// Offline-first: returns cached data immediately, syncs in background
  Future<List<WorkoutTemplate>> getTemplates({bool activeOnly = false}) async {
    final Isar db = _localDb.database;
    final currentUserId = await _authService.getUserId();

    if (currentUserId == null) {
      return [];
    }

    // Load from cache first
    var query = db.workoutTemplates.filter().userIdEqualTo(currentUserId);
    if (activeOnly) {
      query = query.isActiveEqualTo(true);
    }
    final cachedTemplates = await query.sortByCreatedAtDesc().findAll();

    // Sync with server in background if online
    if (_connectivity.isOnline) {
      _syncTemplatesFromServer(db, currentUserId).catchError((e) {
        debugPrint('⚠️ Background sync failed: $e');
      });
    }

    return cachedTemplates;
  }

  /// Get a specific template by ID
  Future<WorkoutTemplate?> getTemplateById(int id) async {
    final Isar db = _localDb.database;

    // Try cache first
    var template = await db.workoutTemplates.get(id);

    // If not in cache and online, fetch from server
    if (template == null && _connectivity.isOnline) {
      try {
        final data = await _apiService.get<Map<String, dynamic>>(
          '${ApiConfig.workoutTemplates}/$id',
        );
        template = WorkoutTemplateJson.fromJson(data);

        // Cache it
        await db.writeTxn(() async {
          await db.workoutTemplates.put(template!);
        });
      } catch (e) {
        debugPrint('Error fetching template: $e');
      }
    }

    return template;
  }

  /// Get community templates (created by others)
  Future<List<WorkoutTemplate>> getCommunityTemplates({
    String? category,
    int? limit = 50,
  }) async {
    final Isar db = _localDb.database;

    // Load from cache first
    var query = db.workoutTemplates.filter().isCommunityEqualTo(true);
    if (category != null) {
      query = query.categoryEqualTo(category);
    }

    var queryBuilder = query.sortByRatingDesc().thenByUsageCountDesc();
    final cachedTemplates =
        limit != null
            ? await queryBuilder.limit(limit).findAll()
            : await queryBuilder.findAll();

    // Sync with server in background if online
    if (_connectivity.isOnline) {
      _syncCommunityTemplatesFromServer(
        db,
        category: category,
        limit: limit,
      ).catchError((e) {
        debugPrint('⚠️ Community templates sync failed: $e');
      });
    }

    return cachedTemplates;
  }

  /// Create a new workout template
  Future<WorkoutTemplate> createTemplate({
    required String name,
    String? description,
    required String exercisesJson,
    required String recurrencePattern,
    String? daysOfWeek,
    int? intervalDays,
    int? estimatedDuration,
    String? category,
  }) async {
    final currentUserId = await _authService.getUserId();

    if (currentUserId == null) {
      throw Exception('User not authenticated');
    }

    final template = WorkoutTemplate(
      userId: currentUserId,
      name: name,
      description: description,
      exercisesJson: exercisesJson,
      recurrencePattern: recurrencePattern,
      daysOfWeek: daysOfWeek,
      intervalDays: intervalDays,
      estimatedDuration: estimatedDuration,
      category: category,
      createdAt: DateTime.now(),
    );

    if (!_connectivity.isOnline) {
      // Offline: save locally with temp ID
      final Isar db = _localDb.database;
      await db.writeTxn(() async {
        await db.workoutTemplates.put(template);
      });
      return template;
    }

    try {
      final data = await _apiService.post<Map<String, dynamic>>(
        ApiConfig.workoutTemplates,
        data: template.toJson(),
      );
      final created = WorkoutTemplateJson.fromJson(data);

      // Cache locally
      final Isar db = _localDb.database;
      await db.writeTxn(() async {
        await db.workoutTemplates.put(created);
      });

      return created;
    } catch (e) {
      debugPrint('Error creating template: $e');
      // Fallback to local-only creation
      final Isar db = _localDb.database;
      await db.writeTxn(() async {
        await db.workoutTemplates.put(template);
      });
      return template;
    }
  }

  /// Update an existing template
  Future<WorkoutTemplate> updateTemplate(WorkoutTemplate template) async {
    template.updatedAt = DateTime.now();

    if (!_connectivity.isOnline) {
      // Offline: update locally
      final Isar db = _localDb.database;
      await db.writeTxn(() async {
        await db.workoutTemplates.put(template);
      });
      return template;
    }

    try {
      final data = await _apiService.put<Map<String, dynamic>>(
        '${ApiConfig.workoutTemplates}/${template.id}',
        data: template.toJson(),
      );
      final updated = WorkoutTemplateJson.fromJson(data);

      // Update cache
      final Isar db = _localDb.database;
      await db.writeTxn(() async {
        await db.workoutTemplates.put(updated);
      });

      return updated;
    } catch (e) {
      debugPrint('Error updating template: $e');
      // Fallback to local-only update
      final Isar db = _localDb.database;
      await db.writeTxn(() async {
        await db.workoutTemplates.put(template);
      });
      return template;
    }
  }

  /// Toggle active status of a template
  Future<void> toggleActive(int id) async {
    final template = await getTemplateById(id);
    if (template == null) return;

    template.isActive = !template.isActive;
    await updateTemplate(template);
  }

  /// Delete a template
  Future<void> deleteTemplate(int id) async {
    if (_connectivity.isOnline) {
      try {
        await _apiService.delete('${ApiConfig.workoutTemplates}/$id');
      } catch (e) {
        debugPrint('Error deleting template from server: $e');
        // Continue with local deletion even if server fails
      }
    }

    // Delete from cache
    final Isar db = _localDb.database;
    await db.writeTxn(() async {
      await db.workoutTemplates.delete(id);
    });
  }

  /// Increment usage count for a template
  Future<void> incrementUsageCount(int id) async {
    final template = await getTemplateById(id);
    if (template == null) return;

    template.usageCount++;
    await updateTemplate(template);

    // Also sync to server in background if online
    if (_connectivity.isOnline) {
      _apiService
          .post('${ApiConfig.workoutTemplates}/$id/increment-usage', data: {})
          .catchError((e) {
            debugPrint('⚠️ Failed to increment usage on server: $e');
          });
    }
  }

  /// Rate a community template
  Future<void> rateTemplate(int id, double rating) async {
    if (rating < 1 || rating > 5) {
      throw Exception('Rating must be between 1 and 5');
    }

    if (!_connectivity.isOnline) {
      throw Exception('Cannot rate while offline');
    }

    try {
      await _apiService.post(
        '${ApiConfig.workoutTemplates}/$id/rate',
        data: {'rating': rating},
      );

      // Refresh the template from server to get updated rating
      final data = await _apiService.get<Map<String, dynamic>>(
        '${ApiConfig.workoutTemplates}/$id',
      );
      final updated = WorkoutTemplateJson.fromJson(data);

      // Update cache
      final Isar db = _localDb.database;
      await db.writeTxn(() async {
        await db.workoutTemplates.put(updated);
      });
    } catch (e) {
      debugPrint('Error rating template: $e');
      rethrow;
    }
  }

  /// Get templates scheduled for a specific date
  Future<List<WorkoutTemplate>> getTemplatesForDate(DateTime date) async {
    final templates = await getTemplates(activeOnly: true);
    final scheduledTemplates = <WorkoutTemplate>[];

    for (final template in templates) {
      if (_isScheduledForDate(template, date)) {
        scheduledTemplates.add(template);
      }
    }

    return scheduledTemplates;
  }

  /// Check if a template is scheduled for a specific date
  bool _isScheduledForDate(WorkoutTemplate template, DateTime date) {
    switch (template.recurrencePattern) {
      case 'daily':
        return true; // Every day

      case 'weekly':
        final weekday = date.weekday; // 1=Mon, 7=Sun
        return template.daysOfWeekList.contains(weekday);

      case 'custom':
        if (template.intervalDays == null) return false;
        // Check if date is a multiple of interval days from creation
        final daysSinceCreation =
            date.difference(template.createdAt).inDays.abs();
        return daysSinceCreation % template.intervalDays! == 0;

      default:
        return false;
    }
  }

  // === PRIVATE HELPERS ===

  /// Sync templates from server to local cache
  Future<void> _syncTemplatesFromServer(Isar db, int userId) async {
    try {
      final data = await _apiService.get<List<dynamic>>(
        '${ApiConfig.workoutTemplates}?userId=$userId',
      );
      final templates =
          data
              .map(
                (json) =>
                    WorkoutTemplateJson.fromJson(json as Map<String, dynamic>),
              )
              .toList();

      // Update cache
      await db.writeTxn(() async {
        for (final template in templates) {
          await db.workoutTemplates.put(template);
        }
      });

      debugPrint('✅ Synced ${templates.length} templates from server');
    } catch (e) {
      debugPrint('⚠️ Failed to sync templates: $e');
      rethrow;
    }
  }

  /// Sync community templates from server
  Future<void> _syncCommunityTemplatesFromServer(
    Isar db, {
    String? category,
    int? limit,
  }) async {
    try {
      var endpoint = '${ApiConfig.workoutTemplates}/community';
      final queryParams = <String>[];

      if (category != null) queryParams.add('category=$category');
      if (limit != null) queryParams.add('limit=$limit');

      if (queryParams.isNotEmpty) {
        endpoint += '?${queryParams.join('&')}';
      }

      final data = await _apiService.get<List<dynamic>>(endpoint);
      final templates =
          data
              .map(
                (json) =>
                    WorkoutTemplateJson.fromJson(json as Map<String, dynamic>),
              )
              .toList();

      // Update cache
      await db.writeTxn(() async {
        for (final template in templates) {
          await db.workoutTemplates.put(template);
        }
      });

      debugPrint('✅ Synced ${templates.length} community templates');
    } catch (e) {
      debugPrint('⚠️ Failed to sync community templates: $e');
      rethrow;
    }
  }
}

// Extension for WorkoutTemplate JSON serialization
extension WorkoutTemplateJson on WorkoutTemplate {
  Map<String, dynamic> toJson() {
    return {
      'userId': userId,
      'name': name,
      'description': description,
      'exercisesJson': exercisesJson,
      'recurrencePattern': recurrencePattern,
      'daysOfWeek': daysOfWeek,
      'intervalDays': intervalDays,
      'estimatedDuration': estimatedDuration,
      'category': category,
      'isActive': isActive,
      'isCommunity': isCommunity,
      'createdByUserId': createdByUserId,
      'usageCount': usageCount,
      'rating': rating,
      'ratingCount': ratingCount,
      'createdAt': createdAt.toIso8601String(),
      'updatedAt': updatedAt?.toIso8601String(),
    };
  }

  static WorkoutTemplate fromJson(Map<String, dynamic> json) {
    return WorkoutTemplate(
      id: json['id'] as int? ?? Isar.autoIncrement,
      userId: json['userId'] as int,
      name: json['name'] as String,
      description: json['description'] as String?,
      exercisesJson: json['exercisesJson'] as String,
      recurrencePattern: json['recurrencePattern'] as String,
      daysOfWeek: json['daysOfWeek'] as String?,
      intervalDays: json['intervalDays'] as int?,
      estimatedDuration: json['estimatedDuration'] as int?,
      category: json['category'] as String?,
      isActive: json['isActive'] as bool? ?? true,
      isCommunity: json['isCommunity'] as bool? ?? false,
      createdByUserId: json['createdByUserId'] as int?,
      usageCount: json['usageCount'] as int? ?? 0,
      rating: (json['rating'] as num?)?.toDouble(),
      ratingCount: json['ratingCount'] as int?,
      createdAt: DateTime.parse(json['createdAt'] as String),
      updatedAt:
          json['updatedAt'] != null
              ? DateTime.parse(json['updatedAt'] as String)
              : null,
    );
  }
}
