// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'exercise.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

Exercise _$ExerciseFromJson(Map<String, dynamic> json) => Exercise(
      id: (json['id'] as num).toInt(),
      sessionId: (json['sessionId'] as num).toInt(),
      name: json['name'] as String,
      duration: (json['duration'] as num?)?.toInt(),
      restTime: (json['restTime'] as num?)?.toInt(),
      notes: json['notes'] as String?,
      exerciseTemplateId: (json['exerciseTemplateId'] as num?)?.toInt(),
      exerciseSets: (json['exerciseSets'] as List<dynamic>?)
              ?.map((e) => ExerciseSet.fromJson(e as Map<String, dynamic>))
              .toList() ??
          const [],
    );

Map<String, dynamic> _$ExerciseToJson(Exercise instance) => <String, dynamic>{
      'id': instance.id,
      'sessionId': instance.sessionId,
      'name': instance.name,
      'duration': instance.duration,
      'restTime': instance.restTime,
      'notes': instance.notes,
      'exerciseTemplateId': instance.exerciseTemplateId,
      'exerciseSets': instance.exerciseSets,
    };
