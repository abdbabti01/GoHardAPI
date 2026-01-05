import 'package:isar/isar.dart';

part 'local_chat_message.g.dart';

/// Local database model for chat messages with offline sync support
@collection
class LocalChatMessage {
  /// Local database ID (auto-increment)
  Id localId = Isar.autoIncrement;

  // ========== Original ChatMessage Fields ==========

  /// Server-side ID (null if not synced yet)
  int? serverId;

  /// Conversation ID (references LocalChatConversation)
  @Index()
  int conversationLocalId; // Local ID reference

  /// Server conversation ID (for sync)
  int? conversationServerId;

  /// Message role: 'user' or 'assistant'
  String role;

  /// Message content
  String content;

  /// When message was created (UTC)
  @Index()
  DateTime createdAt;

  /// Input tokens (for AI messages)
  int? inputTokens;

  /// Output tokens (for AI messages)
  int? outputTokens;

  /// AI model used (for assistant messages)
  String? model;

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
  LocalChatMessage({
    this.serverId,
    required this.conversationLocalId,
    this.conversationServerId,
    required this.role,
    required this.content,
    required this.createdAt,
    this.inputTokens,
    this.outputTokens,
    this.model,
    this.isSynced = false,
    this.syncStatus = 'pending_create',
    required this.lastModifiedLocal,
    this.lastModifiedServer,
    this.syncRetryCount = 0,
    this.lastSyncAttempt,
    this.syncError,
  });
}
