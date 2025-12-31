import 'package:json_annotation/json_annotation.dart';

part 'exercise_set.g.dart';

@JsonSerializable()
class ExerciseSet {
  final int id;
  final int exerciseId;
  final int setNumber;
  final int? reps;
  final double? weight;
  final int? duration;
  final bool isCompleted;
  final DateTime? completedAt;
  final String? notes;

  ExerciseSet({
    required this.id,
    required this.exerciseId,
    required this.setNumber,
    this.reps,
    this.weight,
    this.duration,
    this.isCompleted = false,
    this.completedAt,
    this.notes,
  });

  factory ExerciseSet.fromJson(Map<String, dynamic> json) =>
      _$ExerciseSetFromJson(json);
  Map<String, dynamic> toJson() => _$ExerciseSetToJson(this);
}
