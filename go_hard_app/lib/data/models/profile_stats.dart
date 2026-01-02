import 'package:json_annotation/json_annotation.dart';

part 'profile_stats.g.dart';

@JsonSerializable()
class ProfileStats {
  final int totalWorkouts;
  final int currentStreak;
  final int personalRecords;

  ProfileStats({
    required this.totalWorkouts,
    required this.currentStreak,
    required this.personalRecords,
  });

  factory ProfileStats.fromJson(Map<String, dynamic> json) =>
      _$ProfileStatsFromJson(json);

  Map<String, dynamic> toJson() => _$ProfileStatsToJson(this);
}
