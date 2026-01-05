// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'chat_message.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

ChatMessage _$ChatMessageFromJson(Map<String, dynamic> json) => ChatMessage(
  id: (json['id'] as num).toInt(),
  conversationId: (json['conversationId'] as num).toInt(),
  role: json['role'] as String,
  content: json['content'] as String,
  createdAt: DateTime.parse(json['createdAt'] as String),
  inputTokens: (json['inputTokens'] as num?)?.toInt(),
  outputTokens: (json['outputTokens'] as num?)?.toInt(),
  model: json['model'] as String?,
);

Map<String, dynamic> _$ChatMessageToJson(ChatMessage instance) =>
    <String, dynamic>{
      'id': instance.id,
      'conversationId': instance.conversationId,
      'role': instance.role,
      'content': instance.content,
      'createdAt': instance.createdAt.toIso8601String(),
      'inputTokens': instance.inputTokens,
      'outputTokens': instance.outputTokens,
      'model': instance.model,
    };
