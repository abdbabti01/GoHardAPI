import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';
import '../../../data/models/workout_stats.dart';

class MuscleGroupChart extends StatelessWidget {
  final List<MuscleGroupVolume> data;

  const MuscleGroupChart({super.key, required this.data});

  static const List<Color> _colorPalette = [
    Color(0xFF6366F1), // Indigo
    Color(0xFFEC4899), // Pink
    Color(0xFF10B981), // Green
    Color(0xFFF59E0B), // Amber
    Color(0xFF8B5CF6), // Purple
    Color(0xFF06B6D4), // Cyan
    Color(0xFFEF4444), // Red
    Color(0xFF14B8A6), // Teal
  ];

  @override
  Widget build(BuildContext context) {
    if (data.isEmpty) {
      return const Center(child: Text('No muscle group data available'));
    }

    // Calculate total volume for percentage calculations
    final totalVolume = data.fold<double>(0, (sum, item) => sum + item.volume);

    return Card(
      elevation: 2,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(Icons.pie_chart, color: Theme.of(context).primaryColor),
                const SizedBox(width: 8),
                const Text(
                  'Muscle Group Distribution',
                  style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                ),
              ],
            ),
            const SizedBox(height: 8),
            Text(
              'Last 30 days',
              style: TextStyle(fontSize: 12, color: Colors.grey[600]),
            ),
            const SizedBox(height: 20),
            SizedBox(
              height: 200,
              child: Row(
                children: [
                  Expanded(
                    flex: 3,
                    child: PieChart(
                      PieChartData(
                        sections:
                            data.asMap().entries.map((entry) {
                              final index = entry.key;
                              final muscleGroup = entry.value;
                              final percentage =
                                  (muscleGroup.volume / totalVolume) * 100;
                              final color =
                                  _colorPalette[index % _colorPalette.length];

                              return PieChartSectionData(
                                value: muscleGroup.volume,
                                title:
                                    percentage > 5
                                        ? '${percentage.toStringAsFixed(0)}%'
                                        : '',
                                color: color,
                                radius: 80,
                                titleStyle: const TextStyle(
                                  fontSize: 12,
                                  fontWeight: FontWeight.bold,
                                  color: Colors.white,
                                ),
                              );
                            }).toList(),
                        sectionsSpace: 2,
                        centerSpaceRadius: 0,
                        pieTouchData: PieTouchData(
                          touchCallback:
                              (FlTouchEvent event, pieTouchResponse) {},
                        ),
                      ),
                    ),
                  ),
                  const SizedBox(width: 16),
                  Expanded(
                    flex: 2,
                    child: SingleChildScrollView(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children:
                            data.asMap().entries.map((entry) {
                              final index = entry.key;
                              final muscleGroup = entry.value;
                              final color =
                                  _colorPalette[index % _colorPalette.length];
                              final percentage =
                                  (muscleGroup.volume / totalVolume) * 100;

                              return Padding(
                                padding: const EdgeInsets.only(bottom: 12),
                                child: Row(
                                  children: [
                                    Container(
                                      width: 12,
                                      height: 12,
                                      decoration: BoxDecoration(
                                        color: color,
                                        shape: BoxShape.circle,
                                      ),
                                    ),
                                    const SizedBox(width: 8),
                                    Expanded(
                                      child: Column(
                                        crossAxisAlignment:
                                            CrossAxisAlignment.start,
                                        children: [
                                          Text(
                                            muscleGroup.muscleGroup,
                                            style: const TextStyle(
                                              fontSize: 11,
                                              fontWeight: FontWeight.w600,
                                            ),
                                            maxLines: 1,
                                            overflow: TextOverflow.ellipsis,
                                          ),
                                          Text(
                                            '${(muscleGroup.volume / 1000).toStringAsFixed(1)}k kg (${percentage.toStringAsFixed(0)}%)',
                                            style: TextStyle(
                                              fontSize: 10,
                                              color: Colors.grey[600],
                                            ),
                                          ),
                                        ],
                                      ),
                                    ),
                                  ],
                                ),
                              );
                            }).toList(),
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
