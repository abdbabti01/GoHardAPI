import '../../../data/models/session.dart';

/// Utility class for date-related operations
class DateGroupingUtils {
  /// Group sessions by week
  /// Returns a map where keys are week labels and values are lists of sessions
  static Map<String, List<Session>> groupSessionsByWeek(
    List<Session> sessions,
  ) {
    final Map<String, List<Session>> grouped = {};
    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);

    for (final session in sessions) {
      final sessionDate = DateTime(
        session.date.year,
        session.date.month,
        session.date.day,
      );
      final weekLabel = _getWeekLabel(sessionDate, today);

      if (!grouped.containsKey(weekLabel)) {
        grouped[weekLabel] = [];
      }
      grouped[weekLabel]!.add(session);
    }

    return grouped;
  }

  /// Get week label for a given date relative to today
  static String _getWeekLabel(DateTime date, DateTime today) {
    // Get the start of the week (Monday) for both dates
    final dateWeekStart = _getStartOfWeek(date);
    final todayWeekStart = _getStartOfWeek(today);

    final weeksDifference =
        todayWeekStart.difference(dateWeekStart).inDays ~/ 7;

    if (weeksDifference == 0) {
      return 'This Week';
    } else if (weeksDifference == 1) {
      return 'Last Week';
    } else if (weeksDifference == 2) {
      return '2 Weeks Ago';
    } else if (weeksDifference == 3) {
      return '3 Weeks Ago';
    } else if (weeksDifference < 8) {
      return '$weeksDifference Weeks Ago';
    } else {
      // For older sessions, show month and year
      final monthNames = [
        'January',
        'February',
        'March',
        'April',
        'May',
        'June',
        'July',
        'August',
        'September',
        'October',
        'November',
        'December',
      ];
      return '${monthNames[date.month - 1]} ${date.year}';
    }
  }

  /// Get the start of the week (Monday) for a given date
  static DateTime _getStartOfWeek(DateTime date) {
    // Monday is 1, Sunday is 7
    final weekday = date.weekday;
    return date.subtract(Duration(days: weekday - 1));
  }

  /// Get ordered week labels from the grouped sessions
  /// Returns a list of week labels in chronological order (newest first)
  static List<String> getOrderedWeekLabels(Map<String, List<Session>> grouped) {
    final labels = grouped.keys.toList();

    // Define the order for week labels
    final order = [
      'This Week',
      'Last Week',
      '2 Weeks Ago',
      '3 Weeks Ago',
      '4 Weeks Ago',
      '5 Weeks Ago',
      '6 Weeks Ago',
      '7 Weeks Ago',
    ];

    labels.sort((a, b) {
      // Check if both are in the predefined order
      final aIndex = order.indexOf(a);
      final bIndex = order.indexOf(b);

      if (aIndex != -1 && bIndex != -1) {
        return aIndex.compareTo(bIndex);
      } else if (aIndex != -1) {
        return -1; // a comes first
      } else if (bIndex != -1) {
        return 1; // b comes first
      } else {
        // Both are month/year labels, sort by the most recent session in each group
        final aDate = grouped[a]!.first.date;
        final bDate = grouped[b]!.first.date;
        return bDate.compareTo(aDate); // Descending (newest first)
      }
    });

    return labels;
  }
}
