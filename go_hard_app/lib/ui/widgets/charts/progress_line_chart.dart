import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';
import '../../../data/models/workout_stats.dart';
import 'package:intl/intl.dart';

class ProgressLineChart extends StatelessWidget {
  final List<ProgressDataPoint> data;
  final Color lineColor;
  final String title;

  const ProgressLineChart({
    super.key,
    required this.data,
    this.lineColor = Colors.blue,
    this.title = 'Progress Over Time',
  });

  @override
  Widget build(BuildContext context) {
    if (data.isEmpty) {
      return const Center(child: Text('No progress data available'));
    }

    // Calculate min and max for better scaling
    final values = data.map((d) => d.value).toList();
    final minValue = values.reduce((a, b) => a < b ? a : b);
    final maxValue = values.reduce((a, b) => a > b ? a : b);
    final range = maxValue - minValue;
    final padding = range * 0.1; // 10% padding

    return Card(
      elevation: 2,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(Icons.show_chart, color: lineColor),
                const SizedBox(width: 8),
                Text(
                  title,
                  style: const TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 8),
            Text(
              'Last ${data.length} sessions',
              style: TextStyle(fontSize: 12, color: Colors.grey[600]),
            ),
            const SizedBox(height: 20),
            SizedBox(
              height: 250,
              child: LineChart(
                LineChartData(
                  minY: minValue - padding > 0 ? minValue - padding : 0,
                  maxY: maxValue + padding,
                  gridData: FlGridData(
                    show: true,
                    drawVerticalLine: false,
                    horizontalInterval: null,
                    getDrawingHorizontalLine: (value) {
                      return FlLine(
                        color: Colors.grey.withValues(alpha: 0.2),
                        strokeWidth: 1,
                      );
                    },
                  ),
                  titlesData: FlTitlesData(
                    leftTitles: AxisTitles(
                      sideTitles: SideTitles(
                        showTitles: true,
                        reservedSize: 45,
                        getTitlesWidget: (value, meta) {
                          if (value == meta.max || value == meta.min) {
                            return const Text('');
                          }
                          return Text(
                            '${value.toStringAsFixed(0)} kg',
                            style: const TextStyle(fontSize: 10),
                          );
                        },
                      ),
                    ),
                    rightTitles: const AxisTitles(
                      sideTitles: SideTitles(showTitles: false),
                    ),
                    topTitles: const AxisTitles(
                      sideTitles: SideTitles(showTitles: false),
                    ),
                    bottomTitles: AxisTitles(
                      sideTitles: SideTitles(
                        showTitles: true,
                        reservedSize: 30,
                        interval: 1,
                        getTitlesWidget: (value, meta) {
                          if (value.toInt() >= data.length || value < 0) {
                            return const Text('');
                          }

                          // Show every nth label based on data size
                          final interval =
                              data.length > 15 ? 5 : (data.length > 8 ? 3 : 2);
                          if (value.toInt() % interval != 0 &&
                              value.toInt() != 0 &&
                              value.toInt() != data.length - 1) {
                            return const Text('');
                          }

                          final point = data[value.toInt()];
                          final dateStr = DateFormat('M/d').format(point.date);

                          return Padding(
                            padding: const EdgeInsets.only(top: 8),
                            child: Text(
                              dateStr,
                              style: const TextStyle(fontSize: 9),
                            ),
                          );
                        },
                      ),
                    ),
                  ),
                  borderData: FlBorderData(
                    show: true,
                    border: Border(
                      bottom: BorderSide(
                        color: Colors.grey.withValues(alpha: 0.3),
                      ),
                      left: BorderSide(
                        color: Colors.grey.withValues(alpha: 0.3),
                      ),
                    ),
                  ),
                  lineBarsData: [
                    LineChartBarData(
                      spots:
                          data
                              .asMap()
                              .entries
                              .map(
                                (e) => FlSpot(e.key.toDouble(), e.value.value),
                              )
                              .toList(),
                      isCurved: true,
                      color: lineColor,
                      barWidth: 3,
                      isStrokeCapRound: true,
                      dotData: FlDotData(
                        show: true,
                        getDotPainter: (spot, percent, barData, index) {
                          // Highlight the max value (PR)
                          final isMax = spot.y == maxValue;
                          return FlDotCirclePainter(
                            radius: isMax ? 6 : 4,
                            color: isMax ? Colors.amber : lineColor,
                            strokeWidth: 2,
                            strokeColor: Colors.white,
                          );
                        },
                      ),
                      belowBarData: BarAreaData(
                        show: true,
                        color: lineColor.withValues(alpha: 0.1),
                      ),
                    ),
                  ],
                  lineTouchData: LineTouchData(
                    enabled: true,
                    touchTooltipData: LineTouchTooltipData(
                      getTooltipColor:
                          (touchedSpot) => Colors.black.withValues(alpha: 0.8),
                      tooltipRoundedRadius: 8,
                      getTooltipItems: (touchedSpots) {
                        return touchedSpots.map((spot) {
                          if (spot.spotIndex >= data.length) return null;
                          final point = data[spot.spotIndex];
                          final dateStr = DateFormat(
                            'MMM d, y',
                          ).format(point.date);
                          final isPR = point.value == maxValue;

                          return LineTooltipItem(
                            '${point.value.toStringAsFixed(1)} kg${isPR ? ' 🏆' : ''}\n$dateStr',
                            const TextStyle(
                              color: Colors.white,
                              fontWeight: FontWeight.bold,
                              fontSize: 12,
                            ),
                          );
                        }).toList();
                      },
                    ),
                  ),
                ),
              ),
            ),
            const SizedBox(height: 12),

            // Progress indicator
            _buildProgressIndicator(minValue, maxValue),
          ],
        ),
      ),
    );
  }

  Widget _buildProgressIndicator(double minValue, double maxValue) {
    final firstValue = data.first.value;
    final lastValue = data.last.value;
    final change = lastValue - firstValue;
    final percentChange = firstValue > 0 ? (change / firstValue) * 100 : 0;
    final isPositive = change >= 0;

    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: (isPositive ? Colors.green : Colors.red).withValues(alpha: 0.1),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(
            isPositive ? Icons.trending_up : Icons.trending_down,
            color: isPositive ? Colors.green : Colors.red,
            size: 20,
          ),
          const SizedBox(width: 8),
          Text(
            '${isPositive ? '+' : ''}${change.toStringAsFixed(1)} kg (${isPositive ? '+' : ''}${percentChange.toStringAsFixed(1)}%)',
            style: TextStyle(
              color: isPositive ? Colors.green : Colors.red,
              fontWeight: FontWeight.bold,
              fontSize: 14,
            ),
          ),
          const SizedBox(width: 4),
          Text(
            'from first to last session',
            style: TextStyle(color: Colors.grey[600], fontSize: 11),
          ),
        ],
      ),
    );
  }
}
