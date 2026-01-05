import 'package:isar/isar.dart';

part 'local_chat_conversation.g.dart';

/// Local database model for chat conversations with offline sync support
@collection
class LocalChatConversation {
  /// Local database ID (auto-increment)
  Id localId = Isar.autoIncrement;

  // ========== Original ChatConversation Fields ==========

  /// Server-side ID (null if not synced yet)
  int? serverId;

  /// User ID who owns this conversation
  int userId;

  /// Conversation title
  String title;

  /// Conversation type: 'general', 'workout_plan', 'meal_plan', 'progress_analysis'
  @Index()
  String type;

  /// When conversation was created (UTC)
  DateTime createdAt;

  /// Timestamp of last message (UTC)
  DateTime? lastMessageAt;

  /// Whether conversation is archived
  bool isArchived;

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
  LocalChatConversation({
    this.serverId,
    required this.userId,
    required this.title,
    required this.type,
    required this.createdAt,
    this.lastMessageAt,
    this.isArchived = false,
    this.isSynced = false,
    this.syncStatus = 'pending_create',
    required this.lastModifiedLocal,
    this.lastModifiedServer,
    this.syncRetryCount = 0,
    this.lastSyncAttempt,
    this.syncError,
  });
}
