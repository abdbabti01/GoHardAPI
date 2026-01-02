import 'package:json_annotation/json_annotation.dart';

part 'profile_update_request.g.dart';

@JsonSerializable()
class ProfileUpdateRequest {
  final String? name;
  final String? bio;
  final DateTime? dateOfBirth;
  final String? gender;
  final double? height;
  final double? weight;
  final double? targetWeight;
  final double? bodyFatPercentage;
  final String? experienceLevel;
  final String? primaryGoal;
  final String? goals;
  final String? unitPreference;
  final String? themePreference;
  final String? favoriteExercises;

  ProfileUpdateRequest({
    this.name,
    this.bio,
    this.dateOfBirth,
    this.gender,
    this.height,
    this.weight,
    this.targetWeight,
    this.bodyFatPercentage,
    this.experienceLevel,
    this.primaryGoal,
    this.goals,
    this.unitPreference,
    this.themePreference,
    this.favoriteExercises,
  });

  factory ProfileUpdateRequest.fromJson(Map<String, dynamic> json) =>
      _$ProfileUpdateRequestFromJson(json);

  Map<String, dynamic> toJson() => _$ProfileUpdateRequestToJson(this);
}
