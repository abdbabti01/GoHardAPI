import 'dart:async';
import 'package:flutter/material.dart';
import '../data/models/body_metric.dart';
import '../data/repositories/body_metrics_repository.dart';
import '../core/services/connectivity_service.dart';

/// Provider for body metrics management
class BodyMetricsProvider extends ChangeNotifier {
  final BodyMetricsRepository _bodyMetricsRepository;
  final ConnectivityService? _connectivity;

  List<BodyMetric> _metrics = [];
  BodyMetric? _latestMetric;
  bool _isLoading = false;
  bool _isCreating = false;
  bool _isUpdating = false;
  String? _errorMessage;

  StreamSubscription<bool>? _connectivitySubscription;

  BodyMetricsProvider(this._bodyMetricsRepository, [this._connectivity]) {
    // Listen for connectivity changes and refresh when going online
    _connectivitySubscription = _connectivity?.connectivityStream.listen((
      isOnline,
    ) {
      if (isOnline && _metrics.isEmpty) {
        debugPrint('📡 Connection restored - loading body metrics');
        loadBodyMetrics();
      }
    });
  }

  // Getters
  List<BodyMetric> get metrics => _metrics;
  BodyMetric? get latestMetric => _latestMetric;
  bool get isLoading => _isLoading;
  bool get isCreating => _isCreating;
  bool get isUpdating => _isUpdating;
  String? get errorMessage => _errorMessage;

  /// Load body metrics for the current user
  Future<void> loadBodyMetrics({int days = 90}) async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      _metrics = await _bodyMetricsRepository.getBodyMetrics(days: days);

      if (_metrics.isNotEmpty) {
        // Sort by recordedAt descending and get latest
        _metrics.sort((a, b) => b.recordedAt.compareTo(a.recordedAt));
        _latestMetric = _metrics.first;
      }

      debugPrint('✅ Loaded ${_metrics.length} body metrics');
    } catch (e) {
      _errorMessage =
          'Failed to load body metrics: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Load body metrics error: $e');
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  /// Load the latest body metric entry
  Future<void> loadLatestMetric() async {
    try {
      _latestMetric = await _bodyMetricsRepository.getLatestMetric();
      notifyListeners();
      debugPrint('✅ Loaded latest metric');
    } catch (e) {
      debugPrint('Load latest metric error: $e');
    }
  }

  /// Get a specific body metric by ID
  Future<BodyMetric?> getBodyMetricById(int id) async {
    try {
      return await _bodyMetricsRepository.getBodyMetricById(id);
    } catch (e) {
      _errorMessage =
          'Failed to load metric: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Load metric error: $e');
      notifyListeners();
      return null;
    }
  }

  /// Add a new body metric entry
  Future<bool> createBodyMetric(BodyMetric metric) async {
    _isCreating = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final newMetric = await _bodyMetricsRepository.createBodyMetric(metric);
      _metrics.insert(0, newMetric); // Add to beginning (most recent)
      _latestMetric = newMetric;

      debugPrint('✅ Created body metric entry');
      _isCreating = false;
      notifyListeners();
      return true;
    } catch (e) {
      _errorMessage =
          'Failed to create body metric: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Create body metric error: $e');
      _isCreating = false;
      notifyListeners();
      return false;
    }
  }

  /// Update an existing body metric entry
  Future<bool> updateBodyMetric(int id, BodyMetric metric) async {
    _isUpdating = true;
    _errorMessage = null;
    notifyListeners();

    try {
      await _bodyMetricsRepository.updateBodyMetric(id, metric);

      // Update local list
      final index = _metrics.indexWhere((m) => m.id == id);
      if (index != -1) {
        _metrics[index] = metric;

        // Update latest if this was the latest
        if (_latestMetric?.id == id) {
          _latestMetric = metric;
        }
      }

      debugPrint('✅ Updated body metric: $id');
      _isUpdating = false;
      notifyListeners();
      return true;
    } catch (e) {
      _errorMessage =
          'Failed to update body metric: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Update body metric error: $e');
      _isUpdating = false;
      notifyListeners();
      return false;
    }
  }

  /// Delete a body metric entry
  Future<bool> deleteBodyMetric(int id) async {
    _errorMessage = null;
    notifyListeners();

    try {
      await _bodyMetricsRepository.deleteBodyMetric(id);

      _metrics.removeWhere((m) => m.id == id);

      // Update latest if needed
      if (_latestMetric?.id == id) {
        _latestMetric = _metrics.isNotEmpty ? _metrics.first : null;
      }

      debugPrint('✅ Deleted body metric: $id');
      notifyListeners();
      return true;
    } catch (e) {
      _errorMessage =
          'Failed to delete body metric: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Delete body metric error: $e');
      notifyListeners();
      return false;
    }
  }

  /// Get chart data for a specific metric
  Future<List<Map<String, dynamic>>> getChartData({
    String metric = 'weight',
    int days = 90,
  }) async {
    try {
      return await _bodyMetricsRepository.getChartData(
        metric: metric,
        days: days,
      );
    } catch (e) {
      _errorMessage =
          'Failed to load chart data: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Load chart data error: $e');
      notifyListeners();
      return [];
    }
  }

  /// Clear error message
  void clearError() {
    _errorMessage = null;
    notifyListeners();
  }

  /// Clear all body metrics data (called on logout)
  void clear() {
    _metrics = [];
    _latestMetric = null;
    _errorMessage = null;
    _isLoading = false;
    _isCreating = false;
    _isUpdating = false;
    notifyListeners();
    debugPrint('🧹 BodyMetricsProvider cleared');
  }

  @override
  void dispose() {
    _connectivitySubscription?.cancel();
    super.dispose();
  }
}
