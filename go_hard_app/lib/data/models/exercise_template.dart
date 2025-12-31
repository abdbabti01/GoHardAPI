import 'package:json_annotation/json_annotation.dart';

part 'exercise_template.g.dart';

@JsonSerializable()
class ExerciseTemplate {
  final int id;
  final String name;
  final String? description;
  final String? category;
  final String? muscleGroup;
  final String? equipment;
  final String? difficulty;
  final String? videoUrl;
  final String? imageUrl;
  final String? instructions;
  final bool isCustom;
  final int? createdByUserId;

  ExerciseTemplate({
    required this.id,
    required this.name,
    this.description,
    this.category,
    this.muscleGroup,
    this.equipment,
    this.difficulty,
    this.videoUrl,
    this.imageUrl,
    this.instructions,
    this.isCustom = false,
    this.createdByUserId,
  });

  factory ExerciseTemplate.fromJson(Map<String, dynamic> json) =>
      _$ExerciseTemplateFromJson(json);
  Map<String, dynamic> toJson() => _$ExerciseTemplateToJson(this);
}
