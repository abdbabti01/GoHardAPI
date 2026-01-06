import 'package:json_annotation/json_annotation.dart';

part 'goal_progress.g.dart';

@JsonSerializable()
class GoalProgress {
  final int id;
  final int goalId;
  final DateTime recordedAt;
  final double value;
  final String? notes;

  GoalProgress({
    required this.id,
    required this.goalId,
    required this.recordedAt,
    required this.value,
    this.notes,
  });

  // Helper method to ensure datetime is in UTC
  static DateTime _toUtc(DateTime dt) {
    if (dt.isUtc) return dt;
    return dt.toUtc();
  }

  factory GoalProgress.fromJson(Map<String, dynamic> json) {
    final progress = _$GoalProgressFromJson(json);

    return GoalProgress(
      id: progress.id,
      goalId: progress.goalId,
      recordedAt: _toUtc(progress.recordedAt),
      value: progress.value,
      notes: progress.notes,
    );
  }

  Map<String, dynamic> toJson() => _$GoalProgressToJson(this);

  GoalProgress copyWith({
    int? id,
    int? goalId,
    DateTime? recordedAt,
    double? value,
    String? notes,
  }) {
    return GoalProgress(
      id: id ?? this.id,
      goalId: goalId ?? this.goalId,
      recordedAt: recordedAt ?? this.recordedAt,
      value: value ?? this.value,
      notes: notes ?? this.notes,
    );
  }
}
