import 'package:flutter/foundation.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';
import 'package:permission_handler/permission_handler.dart';
import 'package:timezone/data/latest_all.dart' as tz;
import 'package:timezone/timezone.dart' as tz;
import '../../data/models/session.dart';
import 'debug_logger.dart';

/// Service for managing local notifications
/// Handles daily workout reminders and motivational notifications
class NotificationService {
  static final NotificationService _instance = NotificationService._internal();
  factory NotificationService() => _instance;
  NotificationService._internal();

  final FlutterLocalNotificationsPlugin _notifications =
      FlutterLocalNotificationsPlugin();
  final DebugLogger _logger = DebugLogger();

  bool _isInitialized = false;

  /// Helper to log messages both to console and debug logger
  void _log(String message) {
    _logger.log(message);
  }

  /// Notification IDs
  static const int morningReminderId = 1;
  static const int eveningReminderId = 2;

  /// Initialize notification service
  Future<void> initialize() async {
    if (_isInitialized) return;

    try {
      _log('🔔 Initializing NotificationService...');
      _log('📱 Platform: ${defaultTargetPlatform.name}');

      // Initialize timezone database
      tz.initializeTimeZones();
      _log('🌍 Timezone initialized');

      // Android initialization settings
      const androidSettings = AndroidInitializationSettings(
        '@mipmap/ic_launcher',
      );

      // iOS initialization settings
      const iosSettings = DarwinInitializationSettings(
        requestAlertPermission: true,
        requestBadgePermission: true,
        requestSoundPermission: true,
      );

      const initSettings = InitializationSettings(
        android: androidSettings,
        iOS: iosSettings,
      );

      _log('📝 Notification settings configured');

      // Initialize with callback for when notification is tapped
      final initialized = await _notifications.initialize(
        initSettings,
        onDidReceiveNotificationResponse: _onNotificationTapped,
      );

      _log('🔔 Notification plugin initialized: $initialized');

      // For iOS, explicitly request permissions
      if (defaultTargetPlatform == TargetPlatform.iOS) {
        _log('🍎 Requesting iOS notification permissions...');
        final granted = await _notifications
            .resolvePlatformSpecificImplementation<
              IOSFlutterLocalNotificationsPlugin
            >()
            ?.requestPermissions(alert: true, badge: true, sound: true);
        _log('🍎 iOS permissions granted: $granted');
      }

      _isInitialized = true;
      _log('✅ NotificationService initialized successfully');
    } catch (e, stackTrace) {
      _log('❌ Failed to initialize notifications: $e');
      _log('Stack trace: $stackTrace');
    }
  }

  /// Handle notification tap
  void _onNotificationTapped(NotificationResponse response) {
    _log('🔔 Notification tapped: ${response.payload}');
    // Navigation will be handled by the app router based on payload
  }

  /// Request notification permissions (Android 13+, iOS)
  Future<bool> requestPermissions() async {
    try {
      _log(
        '📱 Requesting notification permissions for ${defaultTargetPlatform.name}',
      );

      if (defaultTargetPlatform == TargetPlatform.android) {
        final status = await Permission.notification.request();
        _log('🤖 Android permission status: ${status.name}');
        return status.isGranted;
      } else if (defaultTargetPlatform == TargetPlatform.iOS) {
        // Request iOS permissions explicitly
        final granted = await _notifications
            .resolvePlatformSpecificImplementation<
              IOSFlutterLocalNotificationsPlugin
            >()
            ?.requestPermissions(alert: true, badge: true, sound: true);
        _log('🍎 iOS permissions granted: $granted');
        return granted ?? false;
      }

      return true;
    } catch (e) {
      _log('❌ Failed to request notification permissions: $e');
      return false;
    }
  }

  /// Schedule morning reminder
  /// Shows planned workouts or motivational message
  Future<void> scheduleMorningReminder({
    required int hour,
    required int minute,
    List<Session>? todayWorkouts,
  }) async {
    await _scheduleDailyNotification(
      id: morningReminderId,
      hour: hour,
      minute: minute,
      title: '💪 GoHard - Daily Reminder',
      body: _getMorningNotificationBody(todayWorkouts),
      payload: 'morning_reminder',
    );

    _log(
      '🔔 Morning reminder scheduled for $hour:${minute.toString().padLeft(2, '0')}',
    );
  }

  /// Schedule evening reminder
  /// Motivates user if they haven't worked out
  Future<void> scheduleEveningReminder({
    required int hour,
    required int minute,
  }) async {
    await _scheduleDailyNotification(
      id: eveningReminderId,
      hour: hour,
      minute: minute,
      title: '🔥 Don\'t Break Your Streak!',
      body: 'You haven\'t worked out today. Even 15 minutes counts!',
      payload: 'evening_reminder',
    );

    _log(
      '🔔 Evening reminder scheduled for $hour:${minute.toString().padLeft(2, '0')}',
    );
  }

  /// Schedule a daily recurring notification
  Future<void> _scheduleDailyNotification({
    required int id,
    required int hour,
    required int minute,
    required String title,
    required String body,
    required String payload,
  }) async {
    try {
      // Cancel existing notification with this ID
      await _notifications.cancel(id);

      // Create notification time for today
      final now = tz.TZDateTime.now(tz.local);
      var scheduledDate = tz.TZDateTime(
        tz.local,
        now.year,
        now.month,
        now.day,
        hour,
        minute,
      );

      // If the time has passed today, schedule for tomorrow
      if (scheduledDate.isBefore(now)) {
        scheduledDate = scheduledDate.add(const Duration(days: 1));
      }

      // Android notification details
      const androidDetails = AndroidNotificationDetails(
        'daily_workout_reminders',
        'Daily Workout Reminders',
        channelDescription: 'Daily notifications to remind you about workouts',
        importance: Importance.high,
        priority: Priority.high,
        icon: '@mipmap/ic_launcher',
      );

      // iOS notification details
      const iosDetails = DarwinNotificationDetails(
        presentAlert: true,
        presentBadge: true,
        presentSound: true,
      );

      const details = NotificationDetails(
        android: androidDetails,
        iOS: iosDetails,
      );

      // Schedule daily notification
      await _notifications.zonedSchedule(
        id,
        title,
        body,
        scheduledDate,
        details,
        uiLocalNotificationDateInterpretation:
            UILocalNotificationDateInterpretation.absoluteTime,
        matchDateTimeComponents: DateTimeComponents.time, // Repeat daily
        androidScheduleMode: AndroidScheduleMode.exactAllowWhileIdle,
      );
    } catch (e) {
      _log('⚠️ Failed to schedule notification: $e');
    }
  }

  /// Generate morning notification body based on today's workouts
  String _getMorningNotificationBody(List<Session>? todayWorkouts) {
    if (todayWorkouts == null || todayWorkouts.isEmpty) {
      return 'No workouts planned today.\n\nPlan a quick session?';
    }

    if (todayWorkouts.length == 1) {
      final workout = todayWorkouts.first;
      return 'You have 1 workout planned:\n• ${workout.name ?? "Workout"}';
    }

    final count = todayWorkouts.length;
    final firstTwo = todayWorkouts.take(2).map((w) => w.name ?? 'Workout');
    return 'You have $count workouts planned:\n• ${firstTwo.join('\n• ')}';
  }

  /// Update morning reminder with latest workout data
  Future<void> updateMorningReminder({
    required int hour,
    required int minute,
    required List<Session> todayWorkouts,
  }) async {
    await scheduleMorningReminder(
      hour: hour,
      minute: minute,
      todayWorkouts: todayWorkouts,
    );
  }

  /// Cancel morning reminder
  Future<void> cancelMorningReminder() async {
    await _notifications.cancel(morningReminderId);
    _log('🔕 Morning reminder cancelled');
  }

  /// Cancel evening reminder
  Future<void> cancelEveningReminder() async {
    await _notifications.cancel(eveningReminderId);
    _log('🔕 Evening reminder cancelled');
  }

  /// Cancel all notifications
  Future<void> cancelAll() async {
    await _notifications.cancelAll();
    _log('🔕 All notifications cancelled');
  }

  /// Show immediate test notification
  Future<void> showTestNotification() async {
    try {
      _log('🔔 Attempting to show test notification...');
      _log('📱 Platform: ${defaultTargetPlatform.name}');
      _log('🔧 Is initialized: $_isInitialized');

      // Check iOS permissions before showing notification
      if (defaultTargetPlatform == TargetPlatform.iOS) {
        final iosImpl =
            _notifications
                .resolvePlatformSpecificImplementation<
                  IOSFlutterLocalNotificationsPlugin
                >();

        if (iosImpl != null) {
          // Check current permission status
          _log('🍎 Checking iOS notification settings...');

          // Request permissions again to ensure they're granted
          final granted = await iosImpl.requestPermissions(
            alert: true,
            badge: true,
            sound: true,
          );
          _log('🍎 iOS permissions check result: $granted');

          if (granted != true) {
            _log('❌ iOS notifications not permitted!');
            throw Exception('iOS notification permissions not granted');
          }
        }
      }

      const androidDetails = AndroidNotificationDetails(
        'test_notifications',
        'Test Notifications',
        channelDescription: 'Test notification channel',
        importance: Importance.max,
        priority: Priority.high,
        playSound: true,
        enableVibration: true,
        enableLights: true,
      );

      const iosDetails = DarwinNotificationDetails(
        presentAlert: true,
        presentBadge: true,
        presentSound: true,
      );

      const details = NotificationDetails(
        android: androidDetails,
        iOS: iosDetails,
      );

      _log('📝 Notification details prepared');
      _log('📤 Calling show() on notification plugin...');

      await _notifications.show(
        999,
        '💪 Test Notification',
        'Your notifications are working!',
        details,
        payload: 'test',
      );

      _log('✅ Test notification show() completed without errors');

      // For iOS, also try to get pending notifications to verify
      if (defaultTargetPlatform == TargetPlatform.iOS) {
        final pending = await _notifications.pendingNotificationRequests();
        _log('📋 Pending notifications: ${pending.length}');
      }
    } catch (e, stackTrace) {
      _log('❌ Error showing test notification: $e');
      _log('Stack trace: $stackTrace');
      rethrow;
    }
  }
}
