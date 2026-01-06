// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'goal.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

Goal _$GoalFromJson(Map<String, dynamic> json) => Goal(
      id: (json['id'] as num).toInt(),
      userId: (json['userId'] as num).toInt(),
      goalType: json['goalType'] as String,
      targetValue: (json['targetValue'] as num).toDouble(),
      currentValue: (json['currentValue'] as num).toDouble(),
      unit: json['unit'] as String?,
      timeFrame: json['timeFrame'] as String?,
      startDate: DateTime.parse(json['startDate'] as String),
      targetDate: json['targetDate'] == null
          ? null
          : DateTime.parse(json['targetDate'] as String),
      isActive: json['isActive'] as bool,
      isCompleted: json['isCompleted'] as bool,
      completedAt: json['completedAt'] == null
          ? null
          : DateTime.parse(json['completedAt'] as String),
      createdAt: DateTime.parse(json['createdAt'] as String),
      progressHistory: (json['progressHistory'] as List<dynamic>?)
          ?.map((e) => GoalProgress.fromJson(e as Map<String, dynamic>))
          .toList(),
    );

Map<String, dynamic> _$GoalToJson(Goal instance) {
  final val = <String, dynamic>{
    'id': instance.id,
    'userId': instance.userId,
    'goalType': instance.goalType,
    'targetValue': instance.targetValue,
    'currentValue': instance.currentValue,
  };

  void writeNotNull(String key, dynamic value) {
    if (value != null) {
      val[key] = value;
    }
  }

  writeNotNull('unit', instance.unit);
  writeNotNull('timeFrame', instance.timeFrame);
  val['startDate'] = instance.startDate.toIso8601String();
  writeNotNull('targetDate', instance.targetDate?.toIso8601String());
  val['isActive'] = instance.isActive;
  val['isCompleted'] = instance.isCompleted;
  writeNotNull('completedAt', instance.completedAt?.toIso8601String());
  val['createdAt'] = instance.createdAt.toIso8601String();
  writeNotNull('progressHistory', instance.progressHistory);
  return val;
}
