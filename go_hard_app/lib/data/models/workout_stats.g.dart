// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'workout_stats.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

WorkoutStats _$WorkoutStatsFromJson(Map<String, dynamic> json) => WorkoutStats(
      totalWorkouts: (json['totalWorkouts'] as num).toInt(),
      totalDuration: (json['totalDuration'] as num).toInt(),
      averageDuration: (json['averageDuration'] as num).toInt(),
      currentStreak: (json['currentStreak'] as num).toInt(),
      longestStreak: (json['longestStreak'] as num).toInt(),
      workoutsThisWeek: (json['workoutsThisWeek'] as num).toInt(),
      workoutsThisMonth: (json['workoutsThisMonth'] as num).toInt(),
      totalSets: (json['totalSets'] as num).toInt(),
      totalReps: (json['totalReps'] as num).toInt(),
      totalVolume: (json['totalVolume'] as num).toDouble(),
      firstWorkoutDate: json['firstWorkoutDate'] == null
          ? null
          : DateTime.parse(json['firstWorkoutDate'] as String),
      lastWorkoutDate: json['lastWorkoutDate'] == null
          ? null
          : DateTime.parse(json['lastWorkoutDate'] as String),
    );

Map<String, dynamic> _$WorkoutStatsToJson(WorkoutStats instance) =>
    <String, dynamic>{
      'totalWorkouts': instance.totalWorkouts,
      'totalDuration': instance.totalDuration,
      'averageDuration': instance.averageDuration,
      'currentStreak': instance.currentStreak,
      'longestStreak': instance.longestStreak,
      'workoutsThisWeek': instance.workoutsThisWeek,
      'workoutsThisMonth': instance.workoutsThisMonth,
      'totalSets': instance.totalSets,
      'totalReps': instance.totalReps,
      'totalVolume': instance.totalVolume,
      'firstWorkoutDate': instance.firstWorkoutDate?.toIso8601String(),
      'lastWorkoutDate': instance.lastWorkoutDate?.toIso8601String(),
    };

ExerciseProgress _$ExerciseProgressFromJson(Map<String, dynamic> json) =>
    ExerciseProgress(
      exerciseTemplateId: (json['exerciseTemplateId'] as num).toInt(),
      exerciseName: json['exerciseName'] as String,
      timesPerformed: (json['timesPerformed'] as num).toInt(),
      totalVolume: (json['totalVolume'] as num).toDouble(),
      personalRecord: (json['personalRecord'] as num?)?.toDouble(),
      personalRecordDate: json['personalRecordDate'] == null
          ? null
          : DateTime.parse(json['personalRecordDate'] as String),
      lastWeight: (json['lastWeight'] as num?)?.toDouble(),
      lastPerformedDate: json['lastPerformedDate'] == null
          ? null
          : DateTime.parse(json['lastPerformedDate'] as String),
      progressPercentage: (json['progressPercentage'] as num?)?.toDouble(),
    );

Map<String, dynamic> _$ExerciseProgressToJson(ExerciseProgress instance) =>
    <String, dynamic>{
      'exerciseTemplateId': instance.exerciseTemplateId,
      'exerciseName': instance.exerciseName,
      'timesPerformed': instance.timesPerformed,
      'totalVolume': instance.totalVolume,
      'personalRecord': instance.personalRecord,
      'personalRecordDate': instance.personalRecordDate?.toIso8601String(),
      'lastWeight': instance.lastWeight,
      'lastPerformedDate': instance.lastPerformedDate?.toIso8601String(),
      'progressPercentage': instance.progressPercentage,
    };

ProgressDataPoint _$ProgressDataPointFromJson(Map<String, dynamic> json) =>
    ProgressDataPoint(
      date: DateTime.parse(json['date'] as String),
      value: (json['value'] as num).toDouble(),
      label: json['label'] as String?,
    );

Map<String, dynamic> _$ProgressDataPointToJson(ProgressDataPoint instance) =>
    <String, dynamic>{
      'date': instance.date.toIso8601String(),
      'value': instance.value,
      'label': instance.label,
    };

MuscleGroupVolume _$MuscleGroupVolumeFromJson(Map<String, dynamic> json) =>
    MuscleGroupVolume(
      muscleGroup: json['muscleGroup'] as String,
      volume: (json['volume'] as num).toDouble(),
      exerciseCount: (json['exerciseCount'] as num).toInt(),
      percentage: (json['percentage'] as num).toDouble(),
    );

Map<String, dynamic> _$MuscleGroupVolumeToJson(MuscleGroupVolume instance) =>
    <String, dynamic>{
      'muscleGroup': instance.muscleGroup,
      'volume': instance.volume,
      'exerciseCount': instance.exerciseCount,
      'percentage': instance.percentage,
    };

PersonalRecord _$PersonalRecordFromJson(Map<String, dynamic> json) =>
    PersonalRecord(
      exerciseName: json['exerciseName'] as String,
      exerciseTemplateId: (json['exerciseTemplateId'] as num).toInt(),
      weight: (json['weight'] as num).toDouble(),
      reps: (json['reps'] as num).toInt(),
      dateAchieved: DateTime.parse(json['dateAchieved'] as String),
      estimatedOneRepMax: (json['estimatedOneRepMax'] as num).toDouble(),
      daysSincePR: (json['daysSincePR'] as num).toInt(),
    );

Map<String, dynamic> _$PersonalRecordToJson(PersonalRecord instance) =>
    <String, dynamic>{
      'exerciseName': instance.exerciseName,
      'exerciseTemplateId': instance.exerciseTemplateId,
      'weight': instance.weight,
      'reps': instance.reps,
      'dateAchieved': instance.dateAchieved.toIso8601String(),
      'estimatedOneRepMax': instance.estimatedOneRepMax,
      'daysSincePR': instance.daysSincePR,
    };
