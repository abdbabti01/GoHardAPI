// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'exercise_set.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

ExerciseSet _$ExerciseSetFromJson(Map<String, dynamic> json) => ExerciseSet(
      id: (json['id'] as num).toInt(),
      exerciseId: (json['exerciseId'] as num).toInt(),
      setNumber: (json['setNumber'] as num).toInt(),
      reps: (json['reps'] as num?)?.toInt(),
      weight: (json['weight'] as num?)?.toDouble(),
      duration: (json['duration'] as num?)?.toInt(),
      isCompleted: json['isCompleted'] as bool? ?? false,
      completedAt: json['completedAt'] == null
          ? null
          : DateTime.parse(json['completedAt'] as String),
      notes: json['notes'] as String?,
    );

Map<String, dynamic> _$ExerciseSetToJson(ExerciseSet instance) =>
    <String, dynamic>{
      'id': instance.id,
      'exerciseId': instance.exerciseId,
      'setNumber': instance.setNumber,
      'reps': instance.reps,
      'weight': instance.weight,
      'duration': instance.duration,
      'isCompleted': instance.isCompleted,
      'completedAt': instance.completedAt?.toIso8601String(),
      'notes': instance.notes,
    };
