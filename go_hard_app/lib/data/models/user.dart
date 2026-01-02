import 'package:json_annotation/json_annotation.dart';
import 'profile_stats.dart';

part 'user.g.dart';

@JsonSerializable()
class User {
  final int id;
  final String name;
  final String email;
  final DateTime dateCreated;

  // Existing fields
  final double? height;
  final double? weight;
  final String? goals;

  // Profile Photo
  final String? profilePhotoUrl;

  // Personal Details
  final String? bio;
  final DateTime? dateOfBirth;
  final int? age; // calculated by backend
  final String? gender;

  // Body Metrics
  final double? targetWeight;
  final double? bodyFatPercentage;
  final double? bmi; // calculated by backend

  // Fitness Profile
  final String? experienceLevel;
  final String? primaryGoal;

  // Preferences
  final String? unitPreference;
  final String? themePreference;

  // Social
  final String? favoriteExercises;

  // Stats
  final ProfileStats? stats;

  User({
    required this.id,
    required this.name,
    required this.email,
    required this.dateCreated,
    this.height,
    this.weight,
    this.goals,
    this.profilePhotoUrl,
    this.bio,
    this.dateOfBirth,
    this.age,
    this.gender,
    this.targetWeight,
    this.bodyFatPercentage,
    this.bmi,
    this.experienceLevel,
    this.primaryGoal,
    this.unitPreference,
    this.themePreference,
    this.favoriteExercises,
    this.stats,
  });

  factory User.fromJson(Map<String, dynamic> json) => _$UserFromJson(json);
  Map<String, dynamic> toJson() => _$UserToJson(this);
}
