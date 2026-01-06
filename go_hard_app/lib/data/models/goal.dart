import 'package:json_annotation/json_annotation.dart';
import 'goal_progress.dart';

part 'goal.g.dart';

@JsonSerializable(includeIfNull: false)
class Goal {
  final int id;
  final int userId;
  final String goalType; // WorkoutFrequency, Weight, Volume, Exercise, BodyFat
  final double targetValue;
  final double currentValue;
  final String? unit;
  final String? timeFrame; // weekly, monthly, total
  final DateTime startDate;
  final DateTime? targetDate;
  final bool isActive;
  final bool isCompleted;
  final DateTime? completedAt;
  final DateTime createdAt;
  final List<GoalProgress>? progressHistory;

  Goal({
    required this.id,
    required this.userId,
    required this.goalType,
    required this.targetValue,
    required this.currentValue,
    this.unit,
    this.timeFrame,
    required this.startDate,
    this.targetDate,
    required this.isActive,
    required this.isCompleted,
    this.completedAt,
    required this.createdAt,
    this.progressHistory,
  });

  // Helper method to ensure datetime is in UTC
  static DateTime _toUtc(DateTime dt) {
    if (dt.isUtc) return dt;
    return dt.toUtc();
  }

  factory Goal.fromJson(Map<String, dynamic> json) {
    final goal = _$GoalFromJson(json);

    return Goal(
      id: goal.id,
      userId: goal.userId,
      goalType: goal.goalType,
      targetValue: goal.targetValue,
      currentValue: goal.currentValue,
      unit: goal.unit,
      timeFrame: goal.timeFrame,
      startDate: _toUtc(goal.startDate),
      targetDate: goal.targetDate != null ? _toUtc(goal.targetDate!) : null,
      isActive: goal.isActive,
      isCompleted: goal.isCompleted,
      completedAt: goal.completedAt != null ? _toUtc(goal.completedAt!) : null,
      createdAt: _toUtc(goal.createdAt),
      progressHistory: goal.progressHistory,
    );
  }

  Map<String, dynamic> toJson() => _$GoalToJson(this);

  double get progressPercentage =>
      targetValue > 0 ? (currentValue / targetValue * 100) : 0;

  Goal copyWith({
    int? id,
    int? userId,
    String? goalType,
    double? targetValue,
    double? currentValue,
    String? unit,
    String? timeFrame,
    DateTime? startDate,
    DateTime? targetDate,
    bool? isActive,
    bool? isCompleted,
    DateTime? completedAt,
    DateTime? createdAt,
    List<GoalProgress>? progressHistory,
  }) {
    return Goal(
      id: id ?? this.id,
      userId: userId ?? this.userId,
      goalType: goalType ?? this.goalType,
      targetValue: targetValue ?? this.targetValue,
      currentValue: currentValue ?? this.currentValue,
      unit: unit ?? this.unit,
      timeFrame: timeFrame ?? this.timeFrame,
      startDate: startDate ?? this.startDate,
      targetDate: targetDate ?? this.targetDate,
      isActive: isActive ?? this.isActive,
      isCompleted: isCompleted ?? this.isCompleted,
      completedAt: completedAt ?? this.completedAt,
      createdAt: createdAt ?? this.createdAt,
      progressHistory: progressHistory ?? this.progressHistory,
    );
  }
}
