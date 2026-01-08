import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import '../core/services/notification_service.dart';

/// Provider for app settings including notification preferences
class SettingsProvider extends ChangeNotifier {
  final FlutterSecureStorage _storage;
  final NotificationService _notificationService;

  // Notification settings
  bool _morningReminderEnabled = true;
  bool _eveningReminderEnabled = true;
  TimeOfDay _morningReminderTime = const TimeOfDay(hour: 8, minute: 0);
  TimeOfDay _eveningReminderTime = const TimeOfDay(hour: 19, minute: 0);

  bool _isLoading = false;

  SettingsProvider(this._storage, this._notificationService) {
    _initialize();
  }

  /// Initialize settings and request permissions
  Future<void> _initialize() async {
    // Request notification permissions first
    await _notificationService.requestPermissions();
    // Then load and schedule notifications
    await loadSettings();
  }

  // Getters
  bool get morningReminderEnabled => _morningReminderEnabled;
  bool get eveningReminderEnabled => _eveningReminderEnabled;
  TimeOfDay get morningReminderTime => _morningReminderTime;
  TimeOfDay get eveningReminderTime => _eveningReminderTime;
  bool get isLoading => _isLoading;

  /// Load settings from storage
  Future<void> loadSettings() async {
    _isLoading = true;
    notifyListeners();

    try {
      // Load morning reminder enabled state
      final morningEnabledStr = await _storage.read(
        key: 'morning_reminder_enabled',
      );
      _morningReminderEnabled = morningEnabledStr != 'false'; // Default true

      // Load evening reminder enabled state
      final eveningEnabledStr = await _storage.read(
        key: 'evening_reminder_enabled',
      );
      _eveningReminderEnabled = eveningEnabledStr != 'false'; // Default true

      // Load morning reminder time
      final morningHourStr = await _storage.read(key: 'morning_reminder_hour');
      final morningMinuteStr = await _storage.read(
        key: 'morning_reminder_minute',
      );
      if (morningHourStr != null && morningMinuteStr != null) {
        _morningReminderTime = TimeOfDay(
          hour: int.parse(morningHourStr),
          minute: int.parse(morningMinuteStr),
        );
      }

      // Load evening reminder time
      final eveningHourStr = await _storage.read(key: 'evening_reminder_hour');
      final eveningMinuteStr = await _storage.read(
        key: 'evening_reminder_minute',
      );
      if (eveningHourStr != null && eveningMinuteStr != null) {
        _eveningReminderTime = TimeOfDay(
          hour: int.parse(eveningHourStr),
          minute: int.parse(eveningMinuteStr),
        );
      }

      debugPrint('Settings loaded successfully');

      // Schedule notifications if enabled
      if (_morningReminderEnabled) {
        await _notificationService.scheduleMorningReminder(
          hour: _morningReminderTime.hour,
          minute: _morningReminderTime.minute,
        );
      }

      if (_eveningReminderEnabled) {
        await _notificationService.scheduleEveningReminder(
          hour: _eveningReminderTime.hour,
          minute: _eveningReminderTime.minute,
        );
      }
    } catch (e) {
      debugPrint('Error loading settings: $e');
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  /// Set morning reminder enabled/disabled
  Future<void> setMorningReminderEnabled(bool enabled) async {
    _morningReminderEnabled = enabled;
    await _storage.write(
      key: 'morning_reminder_enabled',
      value: enabled.toString(),
    );

    if (enabled) {
      await _notificationService.scheduleMorningReminder(
        hour: _morningReminderTime.hour,
        minute: _morningReminderTime.minute,
      );
    } else {
      await _notificationService.cancelMorningReminder();
    }

    notifyListeners();
  }

  /// Set evening reminder enabled/disabled
  Future<void> setEveningReminderEnabled(bool enabled) async {
    _eveningReminderEnabled = enabled;
    await _storage.write(
      key: 'evening_reminder_enabled',
      value: enabled.toString(),
    );

    if (enabled) {
      await _notificationService.scheduleEveningReminder(
        hour: _eveningReminderTime.hour,
        minute: _eveningReminderTime.minute,
      );
    } else {
      await _notificationService.cancelEveningReminder();
    }

    notifyListeners();
  }

  /// Set morning reminder time
  Future<void> setMorningReminderTime(TimeOfDay time) async {
    _morningReminderTime = time;
    await _storage.write(
      key: 'morning_reminder_hour',
      value: time.hour.toString(),
    );
    await _storage.write(
      key: 'morning_reminder_minute',
      value: time.minute.toString(),
    );

    if (_morningReminderEnabled) {
      await _notificationService.scheduleMorningReminder(
        hour: time.hour,
        minute: time.minute,
      );
    }

    notifyListeners();
  }

  /// Set evening reminder time
  Future<void> setEveningReminderTime(TimeOfDay time) async {
    _eveningReminderTime = time;
    await _storage.write(
      key: 'evening_reminder_hour',
      value: time.hour.toString(),
    );
    await _storage.write(
      key: 'evening_reminder_minute',
      value: time.minute.toString(),
    );

    if (_eveningReminderEnabled) {
      await _notificationService.scheduleEveningReminder(
        hour: time.hour,
        minute: time.minute,
      );
    }

    notifyListeners();
  }

  /// Request notification permissions
  Future<bool> requestNotificationPermissions() async {
    return await _notificationService.requestPermissions();
  }

  /// Show test notification
  Future<void> sendTestNotification() async {
    await _notificationService.showTestNotification();
  }
}

extension TimeOfDayFormat on TimeOfDay {
  String get formatted {
    final hour = this.hour.toString().padLeft(2, '0');
    final minute = this.minute.toString().padLeft(2, '0');
    return '$hour:$minute';
  }
}
