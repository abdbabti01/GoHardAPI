import 'package:flutter/foundation.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';
import 'package:permission_handler/permission_handler.dart';
import 'package:timezone/data/latest_all.dart' as tz;
import 'package:timezone/timezone.dart' as tz;
import '../../data/models/session.dart';

/// Service for managing local notifications
/// Handles daily workout reminders and motivational notifications
class NotificationService {
  static final NotificationService _instance = NotificationService._internal();
  factory NotificationService() => _instance;
  NotificationService._internal();

  final FlutterLocalNotificationsPlugin _notifications =
      FlutterLocalNotificationsPlugin();

  bool _isInitialized = false;

  /// Notification IDs
  static const int morningReminderId = 1;
  static const int eveningReminderId = 2;

  /// Initialize notification service
  Future<void> initialize() async {
    if (_isInitialized) return;

    try {
      // Initialize timezone database
      tz.initializeTimeZones();

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

      // Initialize with callback for when notification is tapped
      await _notifications.initialize(
        initSettings,
        onDidReceiveNotificationResponse: _onNotificationTapped,
      );

      _isInitialized = true;
      debugPrint('✅ NotificationService initialized');
    } catch (e) {
      debugPrint('⚠️ Failed to initialize notifications: $e');
    }
  }

  /// Handle notification tap
  void _onNotificationTapped(NotificationResponse response) {
    debugPrint('🔔 Notification tapped: ${response.payload}');
    // Navigation will be handled by the app router based on payload
  }

  /// Request notification permissions (Android 13+)
  Future<bool> requestPermissions() async {
    try {
      if (defaultTargetPlatform == TargetPlatform.android) {
        final status = await Permission.notification.request();
        return status.isGranted;
      }
      // iOS permissions are requested during initialization
      return true;
    } catch (e) {
      debugPrint('⚠️ Failed to request notification permissions: $e');
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

    debugPrint(
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

    debugPrint(
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
      debugPrint('⚠️ Failed to schedule notification: $e');
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
    debugPrint('🔕 Morning reminder cancelled');
  }

  /// Cancel evening reminder
  Future<void> cancelEveningReminder() async {
    await _notifications.cancel(eveningReminderId);
    debugPrint('🔕 Evening reminder cancelled');
  }

  /// Cancel all notifications
  Future<void> cancelAll() async {
    await _notifications.cancelAll();
    debugPrint('🔕 All notifications cancelled');
  }

  /// Show immediate test notification
  Future<void> showTestNotification() async {
    try {
      debugPrint('🔔 Attempting to show test notification...');

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

      await _notifications.show(
        999,
        '💪 Test Notification',
        'Your notifications are working!',
        details,
        payload: 'test',
      );

      debugPrint('✅ Test notification shown successfully');
    } catch (e) {
      debugPrint('❌ Error showing test notification: $e');
      rethrow;
    }
  }
}
