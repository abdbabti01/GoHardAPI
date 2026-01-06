import 'package:flutter/material.dart';
import '../../../data/models/session.dart';

/// Widget displaying workout streak information
class StreakCounter extends StatelessWidget {
  final List<Session> sessions;
  final int weeklyGoal;

  const StreakCounter({super.key, required this.sessions, this.weeklyGoal = 3});

  @override
  Widget build(BuildContext context) {
    final streakData = _calculateStreaks();
    final thisWeekCount = _getThisWeekCount();

    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      elevation: 2,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(
                  Icons.local_fire_department,
                  size: 20,
                  color: Theme.of(context).colorScheme.primary,
                ),
                const SizedBox(width: 8),
                Text(
                  'Streaks & Goals',
                  style: Theme.of(context).textTheme.titleMedium?.copyWith(
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),
            Row(
              children: [
                Expanded(
                  child: _buildStreakItem(
                    context,
                    'Current Streak',
                    streakData['current']!,
                    Icons.whatshot,
                    streakData['current']! > 0 ? Colors.orange : Colors.grey,
                  ),
                ),
                Container(width: 1, height: 50, color: Colors.grey.shade300),
                Expanded(
                  child: _buildStreakItem(
                    context,
                    'Longest Streak',
                    streakData['longest']!,
                    Icons.emoji_events,
                    Colors.amber,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),
            _buildWeeklyGoal(context, thisWeekCount),
          ],
        ),
      ),
    );
  }

  Widget _buildStreakItem(
    BuildContext context,
    String label,
    int value,
    IconData icon,
    Color color,
  ) {
    return Column(
      children: [
        Icon(icon, size: 28, color: color),
        const SizedBox(height: 4),
        Text(
          '$value',
          style: Theme.of(context).textTheme.headlineMedium?.copyWith(
            fontWeight: FontWeight.bold,
            color: color,
          ),
        ),
        Text(
          label,
          style: Theme.of(
            context,
          ).textTheme.bodySmall?.copyWith(color: Colors.grey.shade600),
          textAlign: TextAlign.center,
        ),
        Text(
          'day${value != 1 ? 's' : ''}',
          style: Theme.of(
            context,
          ).textTheme.bodySmall?.copyWith(color: Colors.grey.shade600),
        ),
      ],
    );
  }

  Widget _buildWeeklyGoal(BuildContext context, int thisWeekCount) {
    final progress = thisWeekCount / weeklyGoal;
    final progressClamped = progress > 1.0 ? 1.0 : progress;
    final isGoalMet = thisWeekCount >= weeklyGoal;

    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: isGoalMet ? Colors.green.shade50 : Colors.blue.shade50,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                'Weekly Goal',
                style: TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.w600,
                  color:
                      isGoalMet ? Colors.green.shade700 : Colors.blue.shade700,
                ),
              ),
              Text(
                '$thisWeekCount / $weeklyGoal',
                style: TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.bold,
                  color:
                      isGoalMet ? Colors.green.shade700 : Colors.blue.shade700,
                ),
              ),
            ],
          ),
          const SizedBox(height: 8),
          ClipRRect(
            borderRadius: BorderRadius.circular(4),
            child: LinearProgressIndicator(
              value: progressClamped,
              minHeight: 6,
              backgroundColor: Colors.grey.shade300,
              valueColor: AlwaysStoppedAnimation<Color>(
                isGoalMet ? Colors.green : Colors.blue,
              ),
            ),
          ),
          if (isGoalMet) ...[
            const SizedBox(height: 8),
            Row(
              children: [
                Icon(
                  Icons.check_circle,
                  size: 14,
                  color: Colors.green.shade700,
                ),
                const SizedBox(width: 4),
                Text(
                  'Goal achieved! 🎉',
                  style: TextStyle(
                    fontSize: 12,
                    color: Colors.green.shade700,
                    fontWeight: FontWeight.w500,
                  ),
                ),
              ],
            ),
          ] else if (thisWeekCount > 0) ...[
            const SizedBox(height: 8),
            Text(
              '${weeklyGoal - thisWeekCount} more to reach your goal',
              style: TextStyle(fontSize: 12, color: Colors.blue.shade700),
            ),
          ],
        ],
      ),
    );
  }

  Map<String, int> _calculateStreaks() {
    if (sessions.isEmpty) {
      return {'current': 0, 'longest': 0};
    }

    // Filter completed sessions and sort by date
    final completedSessions =
        sessions.where((s) => s.status == 'completed').toList()
          ..sort((a, b) => b.date.compareTo(a.date));

    if (completedSessions.isEmpty) {
      return {'current': 0, 'longest': 0};
    }

    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);

    // Get unique workout dates
    final workoutDates = <DateTime>{};
    for (final session in completedSessions) {
      workoutDates.add(
        DateTime(session.date.year, session.date.month, session.date.day),
      );
    }

    final sortedDates = workoutDates.toList()..sort((a, b) => b.compareTo(a));

    // Calculate current streak
    int currentStreak = 0;
    DateTime checkDate = today;

    for (final date in sortedDates) {
      if (date == checkDate) {
        currentStreak++;
        checkDate = checkDate.subtract(const Duration(days: 1));
      } else if (date.isBefore(checkDate)) {
        // Gap found, break
        break;
      }
    }

    // Calculate longest streak
    int longestStreak = 0;
    int tempStreak = 1;

    for (int i = 0; i < sortedDates.length - 1; i++) {
      final diff = sortedDates[i].difference(sortedDates[i + 1]).inDays;
      if (diff == 1) {
        tempStreak++;
        if (tempStreak > longestStreak) {
          longestStreak = tempStreak;
        }
      } else {
        tempStreak = 1;
      }
    }

    if (tempStreak > longestStreak) {
      longestStreak = tempStreak;
    }
    if (currentStreak > longestStreak) {
      longestStreak = currentStreak;
    }

    return {'current': currentStreak, 'longest': longestStreak};
  }

  int _getThisWeekCount() {
    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);
    final weekStart = today.subtract(Duration(days: today.weekday - 1));

    return sessions
        .where(
          (s) =>
              s.status == 'completed' &&
              s.date.isAfter(weekStart.subtract(const Duration(days: 1))) &&
              s.date.isBefore(today.add(const Duration(days: 1))),
        )
        .length;
  }
}
