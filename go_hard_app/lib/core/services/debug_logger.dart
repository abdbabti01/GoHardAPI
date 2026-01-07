import 'package:flutter/foundation.dart';

/// Simple debug logger that stores logs in memory
/// Useful for viewing logs on devices where console access is not available
class DebugLogger {
  static final DebugLogger _instance = DebugLogger._internal();
  factory DebugLogger() => _instance;
  DebugLogger._internal();

  final List<String> _logs = [];
  final int _maxLogs = 500; // Keep last 500 logs

  /// Add a log message
  void log(String message) {
    final timestamp = DateTime.now().toString().substring(11, 19);
    final logEntry = '[$timestamp] $message';

    _logs.add(logEntry);

    // Keep only the last _maxLogs entries
    if (_logs.length > _maxLogs) {
      _logs.removeAt(0);
    }

    // Also print to console
    debugPrint(message);
  }

  /// Get all logs
  List<String> get logs => List.unmodifiable(_logs);

  /// Clear all logs
  void clear() {
    _logs.clear();
  }

  /// Get logs as a single string
  String get logsAsString => _logs.join('\n');
}
