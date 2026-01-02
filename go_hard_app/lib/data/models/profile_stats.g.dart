// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'profile_stats.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

ProfileStats _$ProfileStatsFromJson(Map<String, dynamic> json) => ProfileStats(
  totalWorkouts: (json['totalWorkouts'] as num).toInt(),
  currentStreak: (json['currentStreak'] as num).toInt(),
  personalRecords: (json['personalRecords'] as num).toInt(),
);

Map<String, dynamic> _$ProfileStatsToJson(ProfileStats instance) =>
    <String, dynamic>{
      'totalWorkouts': instance.totalWorkouts,
      'currentStreak': instance.currentStreak,
      'personalRecords': instance.personalRecords,
    };
