// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'session.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

Session _$SessionFromJson(Map<String, dynamic> json) => Session(
  id: (json['id'] as num).toInt(),
  userId: (json['userId'] as num).toInt(),
  date: DateTime.parse(json['date'] as String),
  duration: (json['duration'] as num?)?.toInt(),
  notes: json['notes'] as String?,
  type: json['type'] as String?,
  name: json['name'] as String?,
  status: json['status'] as String? ?? 'draft',
  startedAt:
      json['startedAt'] == null
          ? null
          : DateTime.parse(json['startedAt'] as String),
  completedAt:
      json['completedAt'] == null
          ? null
          : DateTime.parse(json['completedAt'] as String),
  pausedAt:
      json['pausedAt'] == null
          ? null
          : DateTime.parse(json['pausedAt'] as String),
  exercises:
      (json['exercises'] as List<dynamic>?)
          ?.map((e) => Exercise.fromJson(e as Map<String, dynamic>))
          .toList() ??
      const [],
);

Map<String, dynamic> _$SessionToJson(Session instance) => <String, dynamic>{
  'id': instance.id,
  'userId': instance.userId,
  'date': instance.date.toIso8601String(),
  'duration': instance.duration,
  'notes': instance.notes,
  'type': instance.type,
  'name': instance.name,
  'status': instance.status,
  'startedAt': instance.startedAt?.toIso8601String(),
  'completedAt': instance.completedAt?.toIso8601String(),
  'pausedAt': instance.pausedAt?.toIso8601String(),
  'exercises': instance.exercises,
};
