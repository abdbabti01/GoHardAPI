import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../../../data/models/session.dart';

/// Calendar heatmap widget showing workout activity
/// Similar to GitHub contribution graph
class CalendarHeatmap extends StatelessWidget {
  final List<Session> sessions;
  final int weeksToShow;

  const CalendarHeatmap({
    super.key,
    required this.sessions,
    this.weeksToShow = 12,
  });

  @override
  Widget build(BuildContext context) {
    final now = DateTime.now();
    final startDate = now.subtract(Duration(days: weeksToShow * 7));

    // Create a map of date -> workout count
    final activityMap = <DateTime, int>{};
    for (final session in sessions) {
      if (session.status == 'completed') {
        final dateKey = DateTime(
          session.date.year,
          session.date.month,
          session.date.day,
        );
        activityMap[dateKey] = (activityMap[dateKey] ?? 0) + 1;
      }
    }

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
                  Icons.calendar_month,
                  size: 20,
                  color: Theme.of(context).colorScheme.primary,
                ),
                const SizedBox(width: 8),
                Text(
                  'Activity',
                  style: Theme.of(context).textTheme.titleMedium?.copyWith(
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),
            _buildHeatmap(context, startDate, now, activityMap),
            const SizedBox(height: 12),
            _buildLegend(context),
          ],
        ),
      ),
    );
  }

  Widget _buildHeatmap(
    BuildContext context,
    DateTime startDate,
    DateTime endDate,
    Map<DateTime, int> activityMap,
  ) {
    // Calculate number of weeks to display
    final days = endDate.difference(startDate).inDays;
    final weeks = (days / 7).ceil();

    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Week day labels
          _buildWeekDayLabels(context),
          const SizedBox(width: 8),
          // Heatmap grid
          Row(
            children: List.generate(weeks, (weekIndex) {
              return Padding(
                padding: const EdgeInsets.only(right: 4),
                child: Column(
                  children: List.generate(7, (dayIndex) {
                    final date = startDate.add(
                      Duration(days: weekIndex * 7 + dayIndex),
                    );
                    if (date.isAfter(endDate)) {
                      return const SizedBox(width: 12, height: 12);
                    }
                    final count = activityMap[date] ?? 0;
                    return _buildDayCell(context, date, count);
                  }),
                ),
              );
            }),
          ),
        ],
      ),
    );
  }

  Widget _buildWeekDayLabels(BuildContext context) {
    final days = ['M', 'T', 'W', 'T', 'F', 'S', 'S'];
    return Column(
      children:
          days.map((day) {
            return SizedBox(
              width: 16,
              height: 12,
              child: Center(
                child: Text(
                  day,
                  style: TextStyle(
                    fontSize: 8,
                    color: Colors.grey.shade600,
                    fontWeight: FontWeight.w500,
                  ),
                ),
              ),
            );
          }).toList(),
    );
  }

  Widget _buildDayCell(BuildContext context, DateTime date, int count) {
    final color = _getColorForCount(count, context);
    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);
    final isToday = date == today;

    return Tooltip(
      message:
          '${DateFormat('MMM d').format(date)}: $count workout${count != 1 ? 's' : ''}',
      child: Container(
        width: 12,
        height: 12,
        margin: const EdgeInsets.only(bottom: 2),
        decoration: BoxDecoration(
          color: color,
          borderRadius: BorderRadius.circular(2),
          border:
              isToday
                  ? Border.all(
                    color: Theme.of(context).colorScheme.primary,
                    width: 1.5,
                  )
                  : null,
        ),
      ),
    );
  }

  Color _getColorForCount(int count, BuildContext context) {
    final primary = Theme.of(context).colorScheme.primary;
    if (count == 0) return Colors.grey.shade200;
    if (count == 1) return primary.withValues(alpha: 0.3);
    if (count == 2) return primary.withValues(alpha: 0.6);
    return primary; // 3+
  }

  Widget _buildLegend(BuildContext context) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.end,
      children: [
        Text(
          'Less',
          style: TextStyle(fontSize: 10, color: Colors.grey.shade600),
        ),
        const SizedBox(width: 4),
        ...List.generate(4, (index) {
          return Padding(
            padding: const EdgeInsets.only(left: 2),
            child: Container(
              width: 10,
              height: 10,
              decoration: BoxDecoration(
                color: _getColorForCount(index, context),
                borderRadius: BorderRadius.circular(2),
              ),
            ),
          );
        }),
        const SizedBox(width: 4),
        Text(
          'More',
          style: TextStyle(fontSize: 10, color: Colors.grey.shade600),
        ),
      ],
    );
  }
}
