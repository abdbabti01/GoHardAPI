import 'package:flutter/material.dart';

/// Represents workout data for a single calendar day
class CalendarDayData {
  final DateTime date;
  final int workoutCount;
  final double totalVolume;
  final int totalDuration;
  final List<int> sessionIds;

  CalendarDayData({
    required this.date,
    this.workoutCount = 0,
    this.totalVolume = 0,
    this.totalDuration = 0,
    this.sessionIds = const [],
  });

  /// Returns true if there was at least one workout on this day
  bool get hasWorkout => workoutCount > 0;

  /// Get color intensity based on workout volume
  /// Returns a value between 0.0 (no workout) and 1.0 (max intensity)
  double getIntensity(double maxVolume) {
    if (maxVolume == 0) return 0;
    return (totalVolume / maxVolume).clamp(0.0, 1.0);
  }

  /// Get color for this day based on intensity
  Color getColor(Color baseColor, double maxVolume) {
    if (!hasWorkout) {
      return Colors.grey.withValues(alpha: 0.1);
    }

    final intensity = getIntensity(maxVolume);

    // Create gradient from light to dark based on intensity
    if (intensity < 0.25) {
      return baseColor.withValues(alpha: 0.3);
    } else if (intensity < 0.5) {
      return baseColor.withValues(alpha: 0.5);
    } else if (intensity < 0.75) {
      return baseColor.withValues(alpha: 0.7);
    } else {
      return baseColor.withValues(alpha: 0.9);
    }
  }

  /// Get formatted summary text
  String getSummary() {
    if (!hasWorkout) return 'No workouts';

    final plural = workoutCount > 1 ? 's' : '';
    final volumeStr =
        totalVolume >= 1000
            ? '${(totalVolume / 1000).toStringAsFixed(1)}k kg'
            : '${totalVolume.toStringAsFixed(0)} kg';

    return '$workoutCount workout$plural • $volumeStr';
  }

  /// Get formatted duration
  String getFormattedDuration() {
    if (totalDuration == 0) return '0 min';
    final hours = totalDuration ~/ 3600;
    final minutes = (totalDuration % 3600) ~/ 60;

    if (hours > 0) {
      return '${hours}h ${minutes}m';
    }
    return '${minutes}m';
  }
}
