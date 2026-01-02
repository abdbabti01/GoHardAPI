import 'package:json_annotation/json_annotation.dart';

part 'workout_stats.g.dart';

@JsonSerializable()
class WorkoutStats {
  final int totalWorkouts;
  final int totalDuration;
  final int averageDuration;
  final int currentStreak;
  final int longestStreak;
  final int workoutsThisWeek;
  final int workoutsThisMonth;
  final int totalSets;
  final int totalReps;
  final double totalVolume;
  final DateTime? firstWorkoutDate;
  final DateTime? lastWorkoutDate;

  WorkoutStats({
    required this.totalWorkouts,
    required this.totalDuration,
    required this.averageDuration,
    required this.currentStreak,
    required this.longestStreak,
    required this.workoutsThisWeek,
    required this.workoutsThisMonth,
    required this.totalSets,
    required this.totalReps,
    required this.totalVolume,
    this.firstWorkoutDate,
    this.lastWorkoutDate,
  });

  factory WorkoutStats.fromJson(Map<String, dynamic> json) =>
      _$WorkoutStatsFromJson(json);

  Map<String, dynamic> toJson() => _$WorkoutStatsToJson(this);

  /// Format duration to human-readable string
  String get formattedTotalDuration {
    final hours = totalDuration ~/ 3600;
    final minutes = (totalDuration % 3600) ~/ 60;
    return '${hours}h ${minutes}m';
  }

  /// Format average duration
  String get formattedAverageDuration {
    final minutes = averageDuration ~/ 60;
    return '$minutes min';
  }

  /// Format total volume
  String get formattedTotalVolume {
    if (totalVolume >= 1000) {
      return '${(totalVolume / 1000).toStringAsFixed(1)}k kg';
    }
    return '${totalVolume.toStringAsFixed(0)} kg';
  }
}

@JsonSerializable()
class ExerciseProgress {
  final int exerciseTemplateId;
  final String exerciseName;
  final int timesPerformed;
  final double totalVolume;
  final double? personalRecord;
  final DateTime? personalRecordDate;
  final double? lastWeight;
  final DateTime? lastPerformedDate;
  final double? progressPercentage;

  ExerciseProgress({
    required this.exerciseTemplateId,
    required this.exerciseName,
    required this.timesPerformed,
    required this.totalVolume,
    this.personalRecord,
    this.personalRecordDate,
    this.lastWeight,
    this.lastPerformedDate,
    this.progressPercentage,
  });

  factory ExerciseProgress.fromJson(Map<String, dynamic> json) =>
      _$ExerciseProgressFromJson(json);

  Map<String, dynamic> toJson() => _$ExerciseProgressToJson(this);

  String get formattedVolume {
    if (totalVolume >= 1000) {
      return '${(totalVolume / 1000).toStringAsFixed(1)}k kg';
    }
    return '${totalVolume.toStringAsFixed(0)} kg';
  }

  String get formattedProgress {
    if (progressPercentage == null) return 'N/A';
    final percent = progressPercentage!;
    final sign = percent >= 0 ? '+' : '';
    return '$sign${percent.toStringAsFixed(1)}%';
  }
}

@JsonSerializable()
class ProgressDataPoint {
  final DateTime date;
  final double value;
  final String? label;

  ProgressDataPoint({required this.date, required this.value, this.label});

  factory ProgressDataPoint.fromJson(Map<String, dynamic> json) =>
      _$ProgressDataPointFromJson(json);

  Map<String, dynamic> toJson() => _$ProgressDataPointToJson(this);
}

@JsonSerializable()
class MuscleGroupVolume {
  final String muscleGroup;
  final double volume;
  final int exerciseCount;
  final double percentage;

  MuscleGroupVolume({
    required this.muscleGroup,
    required this.volume,
    required this.exerciseCount,
    required this.percentage,
  });

  factory MuscleGroupVolume.fromJson(Map<String, dynamic> json) =>
      _$MuscleGroupVolumeFromJson(json);

  Map<String, dynamic> toJson() => _$MuscleGroupVolumeToJson(this);
}

@JsonSerializable()
class PersonalRecord {
  final String exerciseName;
  final int exerciseTemplateId;
  final double weight;
  final int reps;
  final DateTime dateAchieved;
  final double estimatedOneRepMax;
  final int daysSincePR;

  PersonalRecord({
    required this.exerciseName,
    required this.exerciseTemplateId,
    required this.weight,
    required this.reps,
    required this.dateAchieved,
    required this.estimatedOneRepMax,
    required this.daysSincePR,
  });

  factory PersonalRecord.fromJson(Map<String, dynamic> json) =>
      _$PersonalRecordFromJson(json);

  Map<String, dynamic> toJson() => _$PersonalRecordToJson(this);

  String get formattedPR => '${weight.toStringAsFixed(1)} kg × $reps reps';

  String get formattedOneRM => '${estimatedOneRepMax.toStringAsFixed(1)} kg';

  String get timeSincePR {
    if (daysSincePR == 0) return 'Today';
    if (daysSincePR == 1) return 'Yesterday';
    if (daysSincePR < 7) return '$daysSincePR days ago';
    if (daysSincePR < 30) return '${daysSincePR ~/ 7} weeks ago';
    return '${daysSincePR ~/ 30} months ago';
  }
}
