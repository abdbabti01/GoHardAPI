import 'package:isar/isar.dart';

part 'local_exercise_set.g.dart';

/// Local database model for individual exercise sets
@collection
class LocalExerciseSet {
  /// Local database ID (auto-increment)
  Id localId = Isar.autoIncrement;

  // ========== Original ExerciseSet Fields ==========

  /// Server-side ID (null if not synced yet)
  int? serverId;

  /// Local ID of parent exercise
  int exerciseLocalId;

  /// Server ID of parent exercise (for sync reference)
  int? exerciseServerId;

  /// Set number within the exercise
  int setNumber;

  /// Number of repetitions completed
  int? reps;

  /// Weight used in kg or lbs
  double? weight;

  /// Duration of the set in seconds (for timed exercises)
  int? duration;

  /// Whether this set has been marked as completed
  bool isCompleted;

  /// Timestamp when set was completed (UTC)
  DateTime? completedAt;

  /// User notes about this set
  String? notes;

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
  LocalExerciseSet({
    this.serverId,
    required this.exerciseLocalId,
    this.exerciseServerId,
    required this.setNumber,
    this.reps,
    this.weight,
    this.duration,
    this.isCompleted = false,
    this.completedAt,
    this.notes,
    this.isSynced = false,
    this.syncStatus = 'pending_create',
    required this.lastModifiedLocal,
    this.lastModifiedServer,
    this.syncRetryCount = 0,
    this.lastSyncAttempt,
    this.syncError,
  });
}
