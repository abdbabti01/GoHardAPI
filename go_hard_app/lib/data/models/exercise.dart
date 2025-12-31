import 'package:json_annotation/json_annotation.dart';
import 'exercise_set.dart';

part 'exercise.g.dart';

@JsonSerializable()
class Exercise {
  final int id;
  final int sessionId;
  final String name;
  final int? duration;
  final int? restTime;
  final String? notes;
  final int? exerciseTemplateId;
  final List<ExerciseSet> exerciseSets;

  Exercise({
    required this.id,
    required this.sessionId,
    required this.name,
    this.duration,
    this.restTime,
    this.notes,
    this.exerciseTemplateId,
    this.exerciseSets = const [],
  });

  factory Exercise.fromJson(Map<String, dynamic> json) =>
      _$ExerciseFromJson(json);
  Map<String, dynamic> toJson() => _$ExerciseToJson(this);
}
