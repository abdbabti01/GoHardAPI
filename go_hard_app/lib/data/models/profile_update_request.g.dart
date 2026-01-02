// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'profile_update_request.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

ProfileUpdateRequest _$ProfileUpdateRequestFromJson(
  Map<String, dynamic> json,
) => ProfileUpdateRequest(
  name: json['name'] as String?,
  bio: json['bio'] as String?,
  dateOfBirth:
      json['dateOfBirth'] == null
          ? null
          : DateTime.parse(json['dateOfBirth'] as String),
  gender: json['gender'] as String?,
  height: (json['height'] as num?)?.toDouble(),
  weight: (json['weight'] as num?)?.toDouble(),
  targetWeight: (json['targetWeight'] as num?)?.toDouble(),
  bodyFatPercentage: (json['bodyFatPercentage'] as num?)?.toDouble(),
  experienceLevel: json['experienceLevel'] as String?,
  primaryGoal: json['primaryGoal'] as String?,
  goals: json['goals'] as String?,
  unitPreference: json['unitPreference'] as String?,
  themePreference: json['themePreference'] as String?,
  favoriteExercises: json['favoriteExercises'] as String?,
);

Map<String, dynamic> _$ProfileUpdateRequestToJson(
  ProfileUpdateRequest instance,
) => <String, dynamic>{
  'name': instance.name,
  'bio': instance.bio,
  'dateOfBirth': instance.dateOfBirth?.toIso8601String(),
  'gender': instance.gender,
  'height': instance.height,
  'weight': instance.weight,
  'targetWeight': instance.targetWeight,
  'bodyFatPercentage': instance.bodyFatPercentage,
  'experienceLevel': instance.experienceLevel,
  'primaryGoal': instance.primaryGoal,
  'goals': instance.goals,
  'unitPreference': instance.unitPreference,
  'themePreference': instance.themePreference,
  'favoriteExercises': instance.favoriteExercises,
};
