// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'exercise_template.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

ExerciseTemplate _$ExerciseTemplateFromJson(Map<String, dynamic> json) =>
    ExerciseTemplate(
      id: (json['id'] as num).toInt(),
      name: json['name'] as String,
      description: json['description'] as String?,
      category: json['category'] as String?,
      muscleGroup: json['muscleGroup'] as String?,
      equipment: json['equipment'] as String?,
      difficulty: json['difficulty'] as String?,
      videoUrl: json['videoUrl'] as String?,
      imageUrl: json['imageUrl'] as String?,
      instructions: json['instructions'] as String?,
      isCustom: json['isCustom'] as bool? ?? false,
      createdByUserId: (json['createdByUserId'] as num?)?.toInt(),
    );

Map<String, dynamic> _$ExerciseTemplateToJson(ExerciseTemplate instance) =>
    <String, dynamic>{
      'id': instance.id,
      'name': instance.name,
      'description': instance.description,
      'category': instance.category,
      'muscleGroup': instance.muscleGroup,
      'equipment': instance.equipment,
      'difficulty': instance.difficulty,
      'videoUrl': instance.videoUrl,
      'imageUrl': instance.imageUrl,
      'instructions': instance.instructions,
      'isCustom': instance.isCustom,
      'createdByUserId': instance.createdByUserId,
    };
