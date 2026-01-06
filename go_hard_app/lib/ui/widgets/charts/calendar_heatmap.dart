import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../../../data/models/calendar_day_data.dart';

class CalendarHeatmap extends StatefulWidget {
  final Map<DateTime, CalendarDayData> calendarData;
  final Color baseColor;
  final Function(DateTime)? onDayTapped;
  final int monthsToShow;

  const CalendarHeatmap({
    super.key,
    required this.calendarData,
    this.baseColor = Colors.blue,
    this.onDayTapped,
    this.monthsToShow = 3,
  });

  @override
  State<CalendarHeatmap> createState() => _CalendarHeatmapState();
}

class _CalendarHeatmapState extends State<CalendarHeatmap> {
  late DateTime _currentMonth;
  double _maxVolume = 0;

  @override
  void initState() {
    super.initState();
    _currentMonth = DateTime(DateTime.now().year, DateTime.now().month, 1);
    _calculateMaxVolume();
  }

  @override
  void didUpdateWidget(CalendarHeatmap oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.calendarData != widget.calendarData) {
      _calculateMaxVolume();
    }
  }

  void _calculateMaxVolume() {
    _maxVolume = widget.calendarData.values
        .map((d) => d.totalVolume)
        .fold(0.0, (max, val) => val > max ? val : max);
  }

  void _previousMonth() {
    setState(() {
      _currentMonth = DateTime(_currentMonth.year, _currentMonth.month - 1, 1);
    });
  }

  void _nextMonth() {
    setState(() {
      _currentMonth = DateTime(_currentMonth.year, _currentMonth.month + 1, 1);
    });
  }

  void _goToToday() {
    setState(() {
      _currentMonth = DateTime(DateTime.now().year, DateTime.now().month, 1);
    });
  }

  @override
  Widget build(BuildContext context) {
    return Card(
      elevation: 2,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Header
            Row(
              children: [
                Icon(Icons.calendar_month, color: widget.baseColor),
                const SizedBox(width: 8),
                const Text(
                  'Workout Calendar',
                  style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                ),
              ],
            ),
            const SizedBox(height: 16),

            // Month navigation
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                IconButton(
                  icon: const Icon(Icons.chevron_left),
                  onPressed: _previousMonth,
                ),
                Column(
                  children: [
                    Text(
                      DateFormat('MMMM yyyy').format(_currentMonth),
                      style: const TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    TextButton(
                      onPressed: _goToToday,
                      child: const Text(
                        'Today',
                        style: TextStyle(fontSize: 12),
                      ),
                    ),
                  ],
                ),
                IconButton(
                  icon: const Icon(Icons.chevron_right),
                  onPressed: _nextMonth,
                ),
              ],
            ),
            const SizedBox(height: 16),

            // Weekday headers
            _buildWeekdayHeaders(),
            const SizedBox(height: 8),

            // Calendar grid
            _buildCalendarGrid(),
            const SizedBox(height: 16),

            // Legend
            _buildLegend(),
          ],
        ),
      ),
    );
  }

  Widget _buildWeekdayHeaders() {
    final weekdays = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceAround,
      children:
          weekdays.map((day) {
            return Expanded(
              child: Center(
                child: Text(
                  day,
                  style: TextStyle(
                    fontSize: 10,
                    fontWeight: FontWeight.bold,
                    color: Colors.grey[600],
                  ),
                ),
              ),
            );
          }).toList(),
    );
  }

  Widget _buildCalendarGrid() {
    final firstDayOfMonth = _currentMonth;
    final lastDayOfMonth = DateTime(
      _currentMonth.year,
      _currentMonth.month + 1,
      0,
    );
    final daysInMonth = lastDayOfMonth.day;

    // Calculate starting day offset (Monday = 0)
    final firstWeekday = firstDayOfMonth.weekday - 1; // 0-6 where Monday is 0

    // Build calendar cells
    final cells = <Widget>[];

    // Add empty cells for days before month starts
    for (int i = 0; i < firstWeekday; i++) {
      cells.add(_buildEmptyCell());
    }

    // Add cells for each day of the month
    for (int day = 1; day <= daysInMonth; day++) {
      final date = DateTime(_currentMonth.year, _currentMonth.month, day);
      final dateKey = DateTime(date.year, date.month, date.day);
      final dayData = widget.calendarData[dateKey];

      cells.add(_buildDayCell(date, dayData));
    }

    // Calculate number of rows needed
    final totalCells = cells.length;
    final rows = (totalCells / 7).ceil();

    // Fill remaining cells to complete the grid
    final remainingCells = (rows * 7) - totalCells;
    for (int i = 0; i < remainingCells; i++) {
      cells.add(_buildEmptyCell());
    }

    return Column(
      children: List.generate(rows, (rowIndex) {
        return Row(
          children: List.generate(7, (colIndex) {
            final cellIndex = rowIndex * 7 + colIndex;
            return Expanded(child: cells[cellIndex]);
          }),
        );
      }),
    );
  }

  Widget _buildEmptyCell() {
    return Container(
      height: 40,
      margin: const EdgeInsets.all(2),
      decoration: BoxDecoration(
        color: Colors.transparent,
        borderRadius: BorderRadius.circular(4),
      ),
    );
  }

  Widget _buildDayCell(DateTime date, CalendarDayData? dayData) {
    final isToday = _isToday(date);
    final color =
        dayData?.getColor(widget.baseColor, _maxVolume) ??
        Colors.grey.withValues(alpha: 0.1);

    return GestureDetector(
      onTap: () {
        if (dayData != null && dayData.hasWorkout) {
          widget.onDayTapped?.call(date);
          _showDayDetails(context, date, dayData);
        }
      },
      child: Container(
        height: 40,
        margin: const EdgeInsets.all(2),
        decoration: BoxDecoration(
          color: color,
          borderRadius: BorderRadius.circular(4),
          border:
              isToday ? Border.all(color: widget.baseColor, width: 2) : null,
        ),
        child: Center(
          child: Text(
            '${date.day}',
            style: TextStyle(
              fontSize: 12,
              fontWeight: isToday ? FontWeight.bold : FontWeight.normal,
              color:
                  dayData?.hasWorkout == true ? Colors.white : Colors.grey[600],
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildLegend() {
    return Row(
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        const Text('Less', style: TextStyle(fontSize: 11, color: Colors.grey)),
        const SizedBox(width: 8),
        _buildLegendCell(Colors.grey.withValues(alpha: 0.1)),
        _buildLegendCell(widget.baseColor.withValues(alpha: 0.3)),
        _buildLegendCell(widget.baseColor.withValues(alpha: 0.5)),
        _buildLegendCell(widget.baseColor.withValues(alpha: 0.7)),
        _buildLegendCell(widget.baseColor.withValues(alpha: 0.9)),
        const SizedBox(width: 8),
        const Text('More', style: TextStyle(fontSize: 11, color: Colors.grey)),
      ],
    );
  }

  Widget _buildLegendCell(Color color) {
    return Container(
      width: 16,
      height: 16,
      margin: const EdgeInsets.symmetric(horizontal: 2),
      decoration: BoxDecoration(
        color: color,
        borderRadius: BorderRadius.circular(2),
      ),
    );
  }

  bool _isToday(DateTime date) {
    final now = DateTime.now();
    return date.year == now.year &&
        date.month == now.month &&
        date.day == now.day;
  }

  void _showDayDetails(
    BuildContext context,
    DateTime date,
    CalendarDayData dayData,
  ) {
    showDialog(
      context: context,
      builder:
          (context) => AlertDialog(
            title: Text(DateFormat('EEEE, MMM d, y').format(date)),
            content: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Icon(
                      Icons.fitness_center,
                      size: 20,
                      color: widget.baseColor,
                    ),
                    const SizedBox(width: 8),
                    Text(dayData.getSummary()),
                  ],
                ),
                const SizedBox(height: 8),
                Row(
                  children: [
                    const Icon(Icons.timer, size: 20, color: Colors.green),
                    const SizedBox(width: 8),
                    Text('Duration: ${dayData.getFormattedDuration()}'),
                  ],
                ),
                if (dayData.totalVolume > 0) ...[
                  const SizedBox(height: 8),
                  Row(
                    children: [
                      const Icon(
                        Icons.line_weight,
                        size: 20,
                        color: Colors.purple,
                      ),
                      const SizedBox(width: 8),
                      Text(
                        'Volume: ${dayData.totalVolume >= 1000 ? '${(dayData.totalVolume / 1000).toStringAsFixed(1)}k' : dayData.totalVolume.toStringAsFixed(0)} kg',
                      ),
                    ],
                  ),
                ],
              ],
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(context),
                child: const Text('Close'),
              ),
            ],
          ),
    );
  }
}
