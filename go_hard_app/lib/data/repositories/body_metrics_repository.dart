import 'package:flutter/foundation.dart';
import '../../core/constants/api_config.dart';
import '../../core/services/connectivity_service.dart';
import '../models/body_metric.dart';
import '../services/api_service.dart';

/// Repository for body metrics operations with offline caching
class BodyMetricsRepository {
  final ApiService _apiService;
  final ConnectivityService? _connectivity;

  BodyMetricsRepository(this._apiService, [this._connectivity]);

  /// Get body metrics for the current user
  /// Optional parameter: days (default 90 days)
  Future<List<BodyMetric>> getBodyMetrics({int days = 90}) async {
    final isOnline = _connectivity?.isOnline ?? true;

    if (!isOnline) {
      debugPrint(
        '📴 Offline - body metrics feature requires online connection',
      );
      return [];
    }

    try {
      final data = await _apiService.get<List<dynamic>>(
        ApiConfig.bodyMetrics,
        queryParameters: {'days': days.toString()},
      );

      return data
          .map((json) => BodyMetric.fromJson(json as Map<String, dynamic>))
          .toList();
    } catch (e) {
      debugPrint('⚠️ Failed to fetch body metrics: $e');
      rethrow;
    }
  }

  /// Get the latest body metric entry
  Future<BodyMetric?> getLatestMetric() async {
    try {
      final data = await _apiService.get<Map<String, dynamic>>(
        ApiConfig.bodyMetricsLatest,
      );
      return BodyMetric.fromJson(data);
    } catch (e) {
      debugPrint('⚠️ Failed to fetch latest metric: $e');
      return null;
    }
  }

  /// Get a specific body metric by ID
  Future<BodyMetric> getBodyMetricById(int id) async {
    final data = await _apiService.get<Map<String, dynamic>>(
      ApiConfig.bodyMetricById(id),
    );
    return BodyMetric.fromJson(data);
  }

  /// Add a new body metric entry
  Future<BodyMetric> createBodyMetric(BodyMetric metric) async {
    final data = await _apiService.post<Map<String, dynamic>>(
      ApiConfig.bodyMetrics,
      data: metric.toJson(),
    );
    return BodyMetric.fromJson(data);
  }

  /// Update an existing body metric entry
  Future<void> updateBodyMetric(int id, BodyMetric metric) async {
    await _apiService.put<void>(
      ApiConfig.bodyMetricById(id),
      data: metric.toJson(),
    );
  }

  /// Delete a body metric entry
  Future<void> deleteBodyMetric(int id) async {
    await _apiService.delete(ApiConfig.bodyMetricById(id));
  }

  /// Get chart data for a specific metric
  /// metric: weight, bodyfat, chest, waist, hip, arm, thigh, calf
  /// days: number of days to fetch (default 90)
  Future<List<Map<String, dynamic>>> getChartData({
    String metric = 'weight',
    int days = 90,
  }) async {
    try {
      final data = await _apiService.get<List<dynamic>>(
        ApiConfig.bodyMetricsChart,
        queryParameters: {'metric': metric, 'days': days.toString()},
      );

      return data.map((json) => json as Map<String, dynamic>).toList();
    } catch (e) {
      debugPrint('⚠️ Failed to fetch chart data: $e');
      rethrow;
    }
  }
}
