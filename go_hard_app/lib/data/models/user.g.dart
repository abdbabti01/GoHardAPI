// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'user.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

User _$UserFromJson(Map<String, dynamic> json) => User(
  id: (json['id'] as num).toInt(),
  name: json['name'] as String,
  email: json['email'] as String,
  dateCreated: DateTime.parse(json['dateCreated'] as String),
  height: (json['height'] as num?)?.toDouble(),
  weight: (json['weight'] as num?)?.toDouble(),
  goals: json['goals'] as String?,
  profilePhotoUrl: json['profilePhotoUrl'] as String?,
  bio: json['bio'] as String?,
  dateOfBirth:
      json['dateOfBirth'] == null
          ? null
          : DateTime.parse(json['dateOfBirth'] as String),
  age: (json['age'] as num?)?.toInt(),
  gender: json['gender'] as String?,
  targetWeight: (json['targetWeight'] as num?)?.toDouble(),
  bodyFatPercentage: (json['bodyFatPercentage'] as num?)?.toDouble(),
  bmi: (json['bmi'] as num?)?.toDouble(),
  experienceLevel: json['experienceLevel'] as String?,
  primaryGoal: json['primaryGoal'] as String?,
  unitPreference: json['unitPreference'] as String?,
  themePreference: json['themePreference'] as String?,
  favoriteExercises: json['favoriteExercises'] as String?,
  stats:
      json['stats'] == null
          ? null
          : ProfileStats.fromJson(json['stats'] as Map<String, dynamic>),
);

Map<String, dynamic> _$UserToJson(User instance) => <String, dynamic>{
  'id': instance.id,
  'name': instance.name,
  'email': instance.email,
  'dateCreated': instance.dateCreated.toIso8601String(),
  'height': instance.height,
  'weight': instance.weight,
  'goals': instance.goals,
  'profilePhotoUrl': instance.profilePhotoUrl,
  'bio': instance.bio,
  'dateOfBirth': instance.dateOfBirth?.toIso8601String(),
  'age': instance.age,
  'gender': instance.gender,
  'targetWeight': instance.targetWeight,
  'bodyFatPercentage': instance.bodyFatPercentage,
  'bmi': instance.bmi,
  'experienceLevel': instance.experienceLevel,
  'primaryGoal': instance.primaryGoal,
  'unitPreference': instance.unitPreference,
  'themePreference': instance.themePreference,
  'favoriteExercises': instance.favoriteExercises,
  'stats': instance.stats,
};
