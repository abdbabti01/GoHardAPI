import 'package:flutter/material.dart';
import '../../../data/models/session.dart';

/// Widget displaying weekly/monthly progress stats with toggle
class WeeklyProgressCard extends StatefulWidget {
  final List<Session> thisWeekSessions;
  final List<Session> thisMonthSessions;

  const WeeklyProgressCard({
    super.key,
    required this.thisWeekSessions,
    required this.thisMonthSessions,
  });

  @override
  State<WeeklyProgressCard> createState() => _WeeklyProgressCardState();
}

class _WeeklyProgressCardState extends State<WeeklyProgressCard> {
  bool _isMonthlyView = false;

  @override
  Widget build(BuildContext context) {
    // Choose which sessions to display
    final sessions =
        _isMonthlyView ? widget.thisMonthSessions : widget.thisWeekSessions;

    final completed = sessions.where((s) => s.status == 'completed').length;
    final total = sessions.length;
    final percentage = total > 0 ? (completed / total * 100).round() : 0;

    // Calculate total duration and volume
    int totalMinutes = 0;
    for (final session in sessions) {
      if (session.status == 'completed' && session.duration != null) {
        totalMinutes += session.duration!;
      }
    }

    return Card(
      margin: const EdgeInsets.all(16),
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
                  _isMonthlyView ? Icons.calendar_month : Icons.calendar_today,
                  size: 20,
                  color: Theme.of(context).colorScheme.primary,
                ),
                const SizedBox(width: 8),
                Text(
                  _isMonthlyView ? 'This Month' : 'This Week',
                  style: Theme.of(context).textTheme.titleMedium?.copyWith(
                    fontWeight: FontWeight.bold,
                  ),
                ),
                const Spacer(),
                // Toggle button
                Container(
                  decoration: BoxDecoration(
                    color: Theme.of(
                      context,
                    ).colorScheme.primary.withValues(alpha: 0.1),
                    borderRadius: BorderRadius.circular(20),
                  ),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      _buildToggleButton(
                        context,
                        'Week',
                        !_isMonthlyView,
                        () => setState(() => _isMonthlyView = false),
                      ),
                      _buildToggleButton(
                        context,
                        'Month',
                        _isMonthlyView,
                        () => setState(() => _isMonthlyView = true),
                      ),
                    ],
                  ),
                ),
                const SizedBox(width: 8),
                if (completed >= (_isMonthlyView ? 12 : 3))
                  const Text('🔥', style: TextStyle(fontSize: 24)),
              ],
            ),
            const SizedBox(height: 16),

            // Progress bar
            Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Text(
                            '$completed/$total workouts',
                            style: Theme.of(context).textTheme.bodyMedium,
                          ),
                          Text(
                            '$percentage%',
                            style: Theme.of(
                              context,
                            ).textTheme.bodyMedium?.copyWith(
                              fontWeight: FontWeight.bold,
                              color: Theme.of(context).colorScheme.primary,
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 8),
                      ClipRRect(
                        borderRadius: BorderRadius.circular(8),
                        child: LinearProgressIndicator(
                          value: total > 0 ? completed / total : 0,
                          minHeight: 8,
                          backgroundColor: Colors.grey.shade200,
                          valueColor: AlwaysStoppedAnimation<Color>(
                            _getProgressColor(percentage, context),
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),

            const SizedBox(height: 16),

            // Stats row
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceAround,
              children: [
                _buildStat(
                  context,
                  Icons.timer_outlined,
                  '$totalMinutes min',
                  'Total time',
                ),
                Container(height: 30, width: 1, color: Colors.grey.shade300),
                _buildStat(
                  context,
                  Icons.trending_up,
                  '$completed',
                  'Completed',
                ),
              ],
            ),

            // Motivational message
            if (percentage >= 80)
              Padding(
                padding: const EdgeInsets.only(top: 12),
                child: Container(
                  padding: const EdgeInsets.all(8),
                  decoration: BoxDecoration(
                    color: Colors.green.shade50,
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Row(
                    children: [
                      Icon(
                        Icons.emoji_events,
                        size: 16,
                        color: Colors.green.shade700,
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: Text(
                          'Amazing work! Keep it up! 💪',
                          style: TextStyle(
                            fontSize: 12,
                            color: Colors.green.shade700,
                            fontWeight: FontWeight.w500,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }

  Widget _buildStat(
    BuildContext context,
    IconData icon,
    String value,
    String label,
  ) {
    return Column(
      children: [
        Icon(icon, size: 20, color: Theme.of(context).colorScheme.primary),
        const SizedBox(height: 4),
        Text(
          value,
          style: Theme.of(
            context,
          ).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold),
        ),
        Text(
          label,
          style: Theme.of(
            context,
          ).textTheme.bodySmall?.copyWith(color: Colors.grey.shade600),
        ),
      ],
    );
  }

  Color _getProgressColor(int percentage, BuildContext context) {
    if (percentage >= 80) return Colors.green;
    if (percentage >= 50) return Colors.orange;
    return Theme.of(context).colorScheme.primary;
  }

  Widget _buildToggleButton(
    BuildContext context,
    String label,
    bool isSelected,
    VoidCallback onTap,
  ) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
        decoration: BoxDecoration(
          color:
              isSelected
                  ? Theme.of(context).colorScheme.primary
                  : Colors.transparent,
          borderRadius: BorderRadius.circular(16),
        ),
        child: Text(
          label,
          style: TextStyle(
            fontSize: 12,
            fontWeight: FontWeight.w600,
            color:
                isSelected
                    ? Theme.of(context).colorScheme.onPrimary
                    : Theme.of(context).colorScheme.primary,
          ),
        ),
      ),
    );
  }
}
