import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../../providers/settings_provider.dart';

/// Settings screen for managing app preferences
/// Currently focuses on notification settings
class SettingsScreen extends StatelessWidget {
  const SettingsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Settings'), centerTitle: true),
      body: Consumer<SettingsProvider>(
        builder: (context, settings, _) {
          if (settings.isLoading) {
            return const Center(child: CircularProgressIndicator());
          }

          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              // Notifications Section
              _buildSectionHeader(context, 'Notifications'),
              const SizedBox(height: 8),
              _buildNotificationCard(context, settings),
              const SizedBox(height: 24),

              // Test Notification Button
              _buildTestNotificationButton(context, settings),
            ],
          );
        },
      ),
    );
  }

  Widget _buildSectionHeader(BuildContext context, String title) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 8),
      child: Text(
        title,
        style: Theme.of(
          context,
        ).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.bold),
      ),
    );
  }

  Widget _buildNotificationCard(
    BuildContext context,
    SettingsProvider settings,
  ) {
    return Card(
      elevation: 2,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Morning Reminder
            _buildNotificationToggle(
              context,
              title: 'Morning Reminder',
              subtitle:
                  'Daily workout reminder at ${settings.morningReminderTime.formatted}',
              enabled: settings.morningReminderEnabled,
              onChanged: (value) {
                settings.setMorningReminderEnabled(value);
              },
            ),
            if (settings.morningReminderEnabled) ...[
              const SizedBox(height: 8),
              _buildTimePicker(
                context,
                label: 'Morning Time',
                time: settings.morningReminderTime,
                onTimeSelected: (time) {
                  settings.setMorningReminderTime(time);
                },
              ),
            ],
            const Divider(height: 32),

            // Evening Reminder
            _buildNotificationToggle(
              context,
              title: 'Evening Reminder',
              subtitle:
                  'Reminder to complete your workout at ${settings.eveningReminderTime.formatted}',
              enabled: settings.eveningReminderEnabled,
              onChanged: (value) {
                settings.setEveningReminderEnabled(value);
              },
            ),
            if (settings.eveningReminderEnabled) ...[
              const SizedBox(height: 8),
              _buildTimePicker(
                context,
                label: 'Evening Time',
                time: settings.eveningReminderTime,
                onTimeSelected: (time) {
                  settings.setEveningReminderTime(time);
                },
              ),
            ],
          ],
        ),
      ),
    );
  }

  Widget _buildNotificationToggle(
    BuildContext context, {
    required String title,
    required String subtitle,
    required bool enabled,
    required ValueChanged<bool> onChanged,
  }) {
    return SwitchListTile(
      contentPadding: EdgeInsets.zero,
      title: Text(
        title,
        style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 16),
      ),
      subtitle: Text(subtitle),
      value: enabled,
      onChanged: onChanged,
      activeColor: Theme.of(context).primaryColor,
    );
  }

  Widget _buildTimePicker(
    BuildContext context, {
    required String label,
    required TimeOfDay time,
    required ValueChanged<TimeOfDay> onTimeSelected,
  }) {
    return InkWell(
      onTap: () async {
        final selectedTime = await showTimePicker(
          context: context,
          initialTime: time,
          builder: (context, child) {
            return Theme(
              data: Theme.of(context).copyWith(
                timePickerTheme: TimePickerThemeData(
                  backgroundColor: Theme.of(context).cardColor,
                  hourMinuteShape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(8),
                  ),
                ),
              ),
              child: child!,
            );
          },
        );

        if (selectedTime != null) {
          onTimeSelected(selectedTime);
        }
      },
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
        decoration: BoxDecoration(
          color: Theme.of(context).primaryColor.withValues(alpha: 0.1),
          borderRadius: BorderRadius.circular(8),
          border: Border.all(
            color: Theme.of(context).primaryColor.withValues(alpha: 0.3),
          ),
        ),
        child: Row(
          children: [
            Icon(Icons.access_time, color: Theme.of(context).primaryColor),
            const SizedBox(width: 12),
            Text(
              label,
              style: TextStyle(
                fontWeight: FontWeight.w500,
                color: Theme.of(context).primaryColor,
              ),
            ),
            const Spacer(),
            Text(
              time.formatted,
              style: TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.bold,
                color: Theme.of(context).primaryColor,
              ),
            ),
            const SizedBox(width: 8),
            Icon(Icons.arrow_drop_down, color: Theme.of(context).primaryColor),
          ],
        ),
      ),
    );
  }

  Widget _buildTestNotificationButton(
    BuildContext context,
    SettingsProvider settings,
  ) {
    return Card(
      elevation: 2,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Test Notifications',
              style: Theme.of(
                context,
              ).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 8),
            const Text(
              'Send a test notification to verify that notifications are working correctly.',
            ),
            const SizedBox(height: 16),
            SizedBox(
              width: double.infinity,
              child: ElevatedButton.icon(
                onPressed: () async {
                  try {
                    // Request permissions first
                    final granted =
                        await settings.requestNotificationPermissions();

                    if (!granted && context.mounted) {
                      ScaffoldMessenger.of(context).showSnackBar(
                        const SnackBar(
                          content: Text(
                            'Notification permissions are required. Please enable them in app settings.',
                          ),
                          backgroundColor: Colors.orange,
                          duration: Duration(seconds: 5),
                        ),
                      );
                      return;
                    }

                    // Send test notification
                    await settings.sendTestNotification();

                    if (context.mounted) {
                      ScaffoldMessenger.of(context).showSnackBar(
                        const SnackBar(
                          content: Text(
                            'Test notification sent! Check your notification tray.',
                          ),
                          backgroundColor: Colors.green,
                          duration: Duration(seconds: 3),
                        ),
                      );
                    }
                  } catch (e) {
                    if (context.mounted) {
                      ScaffoldMessenger.of(context).showSnackBar(
                        SnackBar(
                          content: Text('Error: $e'),
                          backgroundColor: Colors.red,
                          duration: const Duration(seconds: 5),
                        ),
                      );
                    }
                  }
                },
                icon: const Icon(Icons.notifications_active),
                label: const Text('Send Test Notification'),
                style: ElevatedButton.styleFrom(
                  padding: const EdgeInsets.symmetric(vertical: 12),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
