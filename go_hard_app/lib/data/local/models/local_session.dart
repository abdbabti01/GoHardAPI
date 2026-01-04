import 'package:isar/isar.dart';

part 'local_session.g.dart';

/// Local database model for workout sessions with offline sync support
@collection
class LocalSession {
  /// Local database ID (auto-increment)
  Id localId = Isar.autoIncrement;

  // ========== Original Session Fields ==========

  /// Server-side ID (null if not synced yet)
  int? serverId;

  /// User ID who owns this session
  int userId;

  /// Date of the workout session
  @Index()
  DateTime date;

  /// Duration of the workout in seconds
  int? duration;

  /// User notes about the workout
  String? notes;

  /// Type of workout (e.g., 'strength', 'cardio')
  String? type;

  /// Custom workout name (e.g., 'Pull Day', 'Leg Day')
  String? name;

  /// Session status: 'draft', 'in_progress', 'completed'
  @Index()
  String status;

  /// Timestamp when workout was started (UTC)
  DateTime? startedAt;

  /// Timestamp when workout was completed (UTC)
  DateTime? completedAt;

  /// Timestamp when timer was paused (UTC)
  DateTime? pausedAt;

  // ========== Sync Tracking Fields ==========

  /// Whether entity is in sync with server
  @Index()
  bool isSynced;

  /// Current sync status
  @Index()
  String syncStatus; // 'synced', 'pending_create', 'pending_update', 'pending_delete'

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
  LocalSession({
    this.serverId,
    required this.userId,
    required this.date,
    this.duration,
    this.notes,
    this.type,
    this.name,
    this.status = 'draft',
    this.startedAt,
    this.completedAt,
    this.pausedAt,
    this.isSynced = false,
    this.syncStatus = 'pending_create',
    required this.lastModifiedLocal,
    this.lastModifiedServer,
    this.syncRetryCount = 0,
    this.lastSyncAttempt,
    this.syncError,
  });
}
