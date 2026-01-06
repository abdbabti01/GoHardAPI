// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'goal_progress.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

GoalProgress _$GoalProgressFromJson(Map<String, dynamic> json) => GoalProgress(
  id: (json['id'] as num).toInt(),
  goalId: (json['goalId'] as num).toInt(),
  recordedAt: DateTime.parse(json['recordedAt'] as String),
  value: (json['value'] as num).toDouble(),
  notes: json['notes'] as String?,
);

Map<String, dynamic> _$GoalProgressToJson(GoalProgress instance) =>
    <String, dynamic>{
      'id': instance.id,
      'goalId': instance.goalId,
      'recordedAt': instance.recordedAt.toIso8601String(),
      'value': instance.value,
      'notes': instance.notes,
    };
