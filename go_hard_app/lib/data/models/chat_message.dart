import 'package:json_annotation/json_annotation.dart';

part 'chat_message.g.dart';

@JsonSerializable()
class ChatMessage {
  final int id;
  final int conversationId;
  final String role; // 'user' or 'assistant'
  final String content;
  final DateTime createdAt;
  final int? inputTokens;
  final int? outputTokens;
  final String? model;

  ChatMessage({
    required this.id,
    required this.conversationId,
    required this.role,
    required this.content,
    required this.createdAt,
    this.inputTokens,
    this.outputTokens,
    this.model,
  });

  // Helper method to ensure datetime is in UTC
  static DateTime _toUtc(DateTime dt) {
    if (dt.isUtc) return dt;
    return dt.toUtc();
  }

  factory ChatMessage.fromJson(Map<String, dynamic> json) {
    final message = _$ChatMessageFromJson(json);

    return ChatMessage(
      id: message.id,
      conversationId: message.conversationId,
      role: message.role,
      content: message.content,
      createdAt: _toUtc(message.createdAt),
      inputTokens: message.inputTokens,
      outputTokens: message.outputTokens,
      model: message.model,
    );
  }

  Map<String, dynamic> toJson() => _$ChatMessageToJson(this);

  ChatMessage copyWith({
    int? id,
    int? conversationId,
    String? role,
    String? content,
    DateTime? createdAt,
    int? inputTokens,
    int? outputTokens,
    String? model,
  }) {
    return ChatMessage(
      id: id ?? this.id,
      conversationId: conversationId ?? this.conversationId,
      role: role ?? this.role,
      content: content ?? this.content,
      createdAt: createdAt ?? this.createdAt,
      inputTokens: inputTokens ?? this.inputTokens,
      outputTokens: outputTokens ?? this.outputTokens,
      model: model ?? this.model,
    );
  }
}
