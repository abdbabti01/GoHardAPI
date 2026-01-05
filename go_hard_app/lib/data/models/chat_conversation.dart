import 'package:json_annotation/json_annotation.dart';
import 'chat_message.dart';

part 'chat_conversation.g.dart';

@JsonSerializable()
class ChatConversation {
  final int id;
  final int userId;
  final String title;
  final String
  type; // 'general', 'workout_plan', 'meal_plan', 'progress_analysis'
  final DateTime createdAt;
  final DateTime? lastMessageAt;
  final bool isArchived;
  final List<ChatMessage> messages;
  final int? messageCount; // For list view without loading all messages

  ChatConversation({
    required this.id,
    required this.userId,
    required this.title,
    required this.type,
    required this.createdAt,
    this.lastMessageAt,
    this.isArchived = false,
    this.messages = const [],
    this.messageCount,
  });

  // Helper method to ensure datetime is in UTC
  static DateTime _toUtc(DateTime dt) {
    if (dt.isUtc) return dt;
    return dt.toUtc();
  }

  static DateTime? _toUtcNullable(DateTime? dt) {
    if (dt == null) return null;
    if (dt.isUtc) return dt;
    return dt.toUtc();
  }

  factory ChatConversation.fromJson(Map<String, dynamic> json) {
    final conversation = _$ChatConversationFromJson(json);

    return ChatConversation(
      id: conversation.id,
      userId: conversation.userId,
      title: conversation.title,
      type: conversation.type,
      createdAt: _toUtc(conversation.createdAt),
      lastMessageAt: _toUtcNullable(conversation.lastMessageAt),
      isArchived: conversation.isArchived,
      messages: conversation.messages,
      messageCount: conversation.messageCount,
    );
  }

  Map<String, dynamic> toJson() => _$ChatConversationToJson(this);

  ChatConversation copyWith({
    int? id,
    int? userId,
    String? title,
    String? type,
    DateTime? createdAt,
    DateTime? lastMessageAt,
    bool? isArchived,
    List<ChatMessage>? messages,
    int? messageCount,
  }) {
    return ChatConversation(
      id: id ?? this.id,
      userId: userId ?? this.userId,
      title: title ?? this.title,
      type: type ?? this.type,
      createdAt: createdAt ?? this.createdAt,
      lastMessageAt: lastMessageAt ?? this.lastMessageAt,
      isArchived: isArchived ?? this.isArchived,
      messages: messages ?? this.messages,
      messageCount: messageCount ?? this.messageCount,
    );
  }
}
