import 'dart:async';
import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:flutter/foundation.dart';

/// Service for monitoring network connectivity status
class ConnectivityService extends ChangeNotifier {
  static ConnectivityService? _instance;
  final Connectivity _connectivity = Connectivity();

  StreamSubscription<dynamic>? _connectivitySubscription;
  final StreamController<bool> _connectivityController =
      StreamController<bool>.broadcast();

  bool _isOnline = true;
  bool _isInitialized = false;

  /// Private constructor for singleton pattern
  ConnectivityService._() : super();

  /// Get singleton instance
  static ConnectivityService get instance {
    _instance ??= ConnectivityService._();
    return _instance!;
  }

  /// Initialize connectivity monitoring
  Future<void> initialize() async {
    if (_isInitialized) return;

    // Check initial connectivity
    final initialResult = await _connectivity.checkConnectivity();
    _isOnline = _hasInternetConnectivity([initialResult]);

    // Listen for connectivity changes
    _connectivitySubscription = _connectivity.onConnectivityChanged.listen((
      result,
    ) {
      final wasOnline = _isOnline;
      _isOnline = _hasInternetConnectivity([result]);

      // Only emit if status changed
      if (wasOnline != _isOnline) {
        debugPrint(
          '🌐 Connectivity changed: ${_isOnline ? "ONLINE" : "OFFLINE"}',
        );
        _connectivityController.add(_isOnline);
        notifyListeners(); // Notify widgets to rebuild
      }
    });

    _isInitialized = true;
    debugPrint(
      '✅ ConnectivityService initialized - Status: ${_isOnline ? "ONLINE" : "OFFLINE"}',
    );
  }

  /// Check if any connectivity result indicates internet access
  bool _hasInternetConnectivity(List<ConnectivityResult> results) {
    // If results is empty or contains only 'none', we're offline
    if (results.isEmpty) return false;

    for (final result in results) {
      if (result == ConnectivityResult.mobile ||
          result == ConnectivityResult.wifi ||
          result == ConnectivityResult.ethernet ||
          result == ConnectivityResult.vpn) {
        return true;
      }
    }

    return false;
  }

  /// Get current online status
  bool get isOnline => _isOnline;

  /// Get current offline status
  bool get isOffline => !_isOnline;

  /// Stream of connectivity changes (true = online, false = offline)
  Stream<bool> get connectivityStream => _connectivityController.stream;

  /// Wait for online connectivity (useful for sync operations)
  /// Returns immediately if already online
  /// Timeout after specified duration if still offline
  Future<bool> waitForConnection({
    Duration timeout = const Duration(seconds: 30),
  }) async {
    if (_isOnline) return true;

    try {
      final result = await connectivityStream
          .firstWhere((isOnline) => isOnline)
          .timeout(timeout);
      return result;
    } on TimeoutException {
      debugPrint('⏱️ Timeout waiting for connection');
      return false;
    }
  }

  /// Manually check connectivity (useful for testing)
  Future<bool> checkConnectivity() async {
    final result = await _connectivity.checkConnectivity();
    _isOnline = _hasInternetConnectivity([result]);
    return _isOnline;
  }

  /// Dispose resources
  @override
  void dispose() {
    _connectivitySubscription?.cancel();
    _connectivityController.close();
    _isInitialized = false;
    super.dispose();
  }

  /// Reset singleton (useful for testing)
  static void reset() {
    _instance?.dispose();
    _instance = null;
  }
}
