import 'package:json_annotation/json_annotation.dart';

part 'body_metric.g.dart';

@JsonSerializable()
class BodyMetric {
  final int id;
  final int userId;
  final DateTime recordedAt;
  final double? weight;
  final double? bodyFatPercentage;
  final double? chestCircumference;
  final double? waistCircumference;
  final double? hipCircumference;
  final double? armCircumference;
  final double? thighCircumference;
  final double? calfCircumference;
  final String? notes;
  final String? photoUrl;
  final DateTime createdAt;

  BodyMetric({
    required this.id,
    required this.userId,
    required this.recordedAt,
    this.weight,
    this.bodyFatPercentage,
    this.chestCircumference,
    this.waistCircumference,
    this.hipCircumference,
    this.armCircumference,
    this.thighCircumference,
    this.calfCircumference,
    this.notes,
    this.photoUrl,
    required this.createdAt,
  });

  // Helper method to ensure datetime is in UTC
  static DateTime _toUtc(DateTime dt) {
    if (dt.isUtc) return dt;
    return dt.toUtc();
  }

  factory BodyMetric.fromJson(Map<String, dynamic> json) {
    final metric = _$BodyMetricFromJson(json);

    return BodyMetric(
      id: metric.id,
      userId: metric.userId,
      recordedAt: _toUtc(metric.recordedAt),
      weight: metric.weight,
      bodyFatPercentage: metric.bodyFatPercentage,
      chestCircumference: metric.chestCircumference,
      waistCircumference: metric.waistCircumference,
      hipCircumference: metric.hipCircumference,
      armCircumference: metric.armCircumference,
      thighCircumference: metric.thighCircumference,
      calfCircumference: metric.calfCircumference,
      notes: metric.notes,
      photoUrl: metric.photoUrl,
      createdAt: _toUtc(metric.createdAt),
    );
  }

  Map<String, dynamic> toJson() => _$BodyMetricToJson(this);

  BodyMetric copyWith({
    int? id,
    int? userId,
    DateTime? recordedAt,
    double? weight,
    double? bodyFatPercentage,
    double? chestCircumference,
    double? waistCircumference,
    double? hipCircumference,
    double? armCircumference,
    double? thighCircumference,
    double? calfCircumference,
    String? notes,
    String? photoUrl,
    DateTime? createdAt,
  }) {
    return BodyMetric(
      id: id ?? this.id,
      userId: userId ?? this.userId,
      recordedAt: recordedAt ?? this.recordedAt,
      weight: weight ?? this.weight,
      bodyFatPercentage: bodyFatPercentage ?? this.bodyFatPercentage,
      chestCircumference: chestCircumference ?? this.chestCircumference,
      waistCircumference: waistCircumference ?? this.waistCircumference,
      hipCircumference: hipCircumference ?? this.hipCircumference,
      armCircumference: armCircumference ?? this.armCircumference,
      thighCircumference: thighCircumference ?? this.thighCircumference,
      calfCircumference: calfCircumference ?? this.calfCircumference,
      notes: notes ?? this.notes,
      photoUrl: photoUrl ?? this.photoUrl,
      createdAt: createdAt ?? this.createdAt,
    );
  }
}
