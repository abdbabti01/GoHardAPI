import 'package:isar/isar.dart';

part 'local_exercise.g.dart';

/// Local database model for exercises within a workout session
@collection
class LocalExercise {
  /// Local database ID (auto-increment)
  Id localId = Isar.autoIncrement;

  // ========== Original Exercise Fields ==========

  /// Server-side ID (null if not synced yet)
  int? serverId;

  /// Local ID of parent session
  int sessionLocalId;

  /// Server ID of parent session (for sync reference)
  int? sessionServerId;

  /// Exercise name
  String name;

  /// Duration in seconds
  int? duration;

  /// Rest time between sets in seconds
  int? restTime;

  /// User notes about this exercise
  String? notes;

  /// Reference to exercise template ID
  int? exerciseTemplateId;

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
  LocalExercise({
    this.serverId,
    required this.sessionLocalId,
    this.sessionServerId,
    required this.name,
    this.duration,
    this.restTime,
    this.notes,
    this.exerciseTemplateId,
    this.isSynced = false,
    this.syncStatus = 'pending_create',
    required this.lastModifiedLocal,
    this.lastModifiedServer,
    this.syncRetryCount = 0,
    this.lastSyncAttempt,
    this.syncError,
  });
}
