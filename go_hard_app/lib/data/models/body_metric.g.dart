// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'body_metric.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

BodyMetric _$BodyMetricFromJson(Map<String, dynamic> json) => BodyMetric(
      id: (json['id'] as num).toInt(),
      userId: (json['userId'] as num).toInt(),
      recordedAt: DateTime.parse(json['recordedAt'] as String),
      weight: (json['weight'] as num?)?.toDouble(),
      bodyFatPercentage: (json['bodyFatPercentage'] as num?)?.toDouble(),
      chestCircumference: (json['chestCircumference'] as num?)?.toDouble(),
      waistCircumference: (json['waistCircumference'] as num?)?.toDouble(),
      hipCircumference: (json['hipCircumference'] as num?)?.toDouble(),
      armCircumference: (json['armCircumference'] as num?)?.toDouble(),
      thighCircumference: (json['thighCircumference'] as num?)?.toDouble(),
      calfCircumference: (json['calfCircumference'] as num?)?.toDouble(),
      notes: json['notes'] as String?,
      photoUrl: json['photoUrl'] as String?,
      createdAt: DateTime.parse(json['createdAt'] as String),
    );

Map<String, dynamic> _$BodyMetricToJson(BodyMetric instance) =>
    <String, dynamic>{
      'id': instance.id,
      'userId': instance.userId,
      'recordedAt': instance.recordedAt.toIso8601String(),
      'weight': instance.weight,
      'bodyFatPercentage': instance.bodyFatPercentage,
      'chestCircumference': instance.chestCircumference,
      'waistCircumference': instance.waistCircumference,
      'hipCircumference': instance.hipCircumference,
      'armCircumference': instance.armCircumference,
      'thighCircumference': instance.thighCircumference,
      'calfCircumference': instance.calfCircumference,
      'notes': instance.notes,
      'photoUrl': instance.photoUrl,
      'createdAt': instance.createdAt.toIso8601String(),
    };
