import 'package:isar/isar.dart';

part 'local_exercise_template.g.dart';

/// Local database model for exercise templates (system and user-created)
@collection
class LocalExerciseTemplate {
  /// Local database ID (auto-increment)
  Id localId = Isar.autoIncrement;

  // ========== Original ExerciseTemplate Fields ==========

  /// Server-side ID (null if not synced yet)
  int? serverId;

  /// Exercise name
  @Index()
  String name;

  /// Exercise description
  String? description;

  /// Exercise category (e.g., 'strength', 'cardio')
  @Index()
  String? category;

  /// Primary muscle group targeted
  @Index()
  String? muscleGroup;

  /// Equipment needed
  String? equipment;

  /// Difficulty level (e.g., 'beginner', 'intermediate', 'advanced')
  String? difficulty;

  /// URL to instructional video
  String? videoUrl;

  /// URL to exercise image
  String? imageUrl;

  /// Step-by-step instructions
  String? instructions;

  /// Whether this is a user-created template (vs system template)
  @Index()
  bool isCustom;

  /// User ID who created this template (null for system templates)
  int? createdByUserId;

  // ========== Sync Tracking Fields ==========

  /// Whether entity is in sync with server
  @Index()
  bool isSynced;

  /// Current sync status
  @Index()
  String syncStatus;

  /// Timestamp of last local modification
  DateTime lastModifiedLocal;

  /// Timestamp of last server modification (from API response)
  DateTime? lastModifiedServer;

  /// Number of failed sync attempts
  int syncRetryCount;

  /// Timestamp of last sync attempt
  DateTime? lastSyncAttempt;

  /// Error message from last failed sync
  String? syncError;

  /// Constructor
  LocalExerciseTemplate({
    this.serverId,
    required this.name,
    this.description,
    this.category,
    this.muscleGroup,
    this.equipment,
    this.difficulty,
    this.videoUrl,
    this.imageUrl,
    this.instructions,
    this.isCustom = false,
    this.createdByUserId,
    this.isSynced = false,
    this.syncStatus = 'pending_create',
    required this.lastModifiedLocal,
    this.lastModifiedServer,
    this.syncRetryCount = 0,
    this.lastSyncAttempt,
    this.syncError,
  });
}
