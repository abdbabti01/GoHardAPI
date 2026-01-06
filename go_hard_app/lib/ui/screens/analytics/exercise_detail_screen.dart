import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../../providers/analytics_provider.dart';
import '../../../data/models/workout_stats.dart';
import '../../widgets/charts/progress_line_chart.dart';

class ExerciseDetailScreen extends StatefulWidget {
  final ExerciseProgress exercise;

  const ExerciseDetailScreen({super.key, required this.exercise});

  @override
  State<ExerciseDetailScreen> createState() => _ExerciseDetailScreenState();
}

class _ExerciseDetailScreenState extends State<ExerciseDetailScreen>
    with SingleTickerProviderStateMixin {
  late TabController _tabController;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 2, vsync: this);
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final provider = context.watch<AnalyticsProvider>();

    return Scaffold(
      appBar: AppBar(
        title: Text(widget.exercise.exerciseName),
        bottom: TabBar(
          controller: _tabController,
          tabs: const [
            Tab(text: 'Progress', icon: Icon(Icons.trending_up)),
            Tab(text: 'Stats', icon: Icon(Icons.bar_chart)),
          ],
        ),
      ),
      body: Column(
        children: [
          // Summary Card
          _buildSummaryCard(),

          // Tab Content
          Expanded(
            child: TabBarView(
              controller: _tabController,
              children: [_buildProgressTab(provider), _buildStatsTab()],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildSummaryCard() {
    return Card(
      margin: const EdgeInsets.all(16),
      elevation: 2,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceAround,
              children: [
                _buildSummaryStat(
                  'Times Performed',
                  widget.exercise.timesPerformed.toString(),
                  Icons.fitness_center,
                  Colors.blue,
                ),
                _buildSummaryStat(
                  'Total Volume',
                  widget.exercise.formattedVolume,
                  Icons.line_weight,
                  Colors.green,
                ),
              ],
            ),
            const SizedBox(height: 16),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceAround,
              children: [
                _buildSummaryStat(
                  'Personal Record',
                  widget.exercise.personalRecord != null
                      ? '${widget.exercise.personalRecord!.toStringAsFixed(1)} kg'
                      : 'N/A',
                  Icons.emoji_events,
                  Colors.amber,
                ),
                if (widget.exercise.progressPercentage != null)
                  _buildSummaryStat(
                    'Progress',
                    widget.exercise.formattedProgress,
                    widget.exercise.progressPercentage! >= 0
                        ? Icons.trending_up
                        : Icons.trending_down,
                    widget.exercise.progressPercentage! >= 0
                        ? Colors.green
                        : Colors.red,
                  )
                else
                  _buildSummaryStat(
                    'Last Weight',
                    widget.exercise.lastWeight != null
                        ? '${widget.exercise.lastWeight!.toStringAsFixed(1)} kg'
                        : 'N/A',
                    Icons.scale,
                    Colors.purple,
                  ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildSummaryStat(
    String label,
    String value,
    IconData icon,
    Color color,
  ) {
    return Column(
      children: [
        Icon(icon, color: color, size: 24),
        const SizedBox(height: 4),
        Text(
          value,
          style: TextStyle(
            fontSize: 18,
            fontWeight: FontWeight.bold,
            color: color,
          ),
        ),
        Text(
          label,
          style: const TextStyle(fontSize: 11, color: Colors.grey),
          textAlign: TextAlign.center,
        ),
      ],
    );
  }

  Widget _buildProgressTab(AnalyticsProvider provider) {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'Weight Progression',
            style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 16),

          // Weight progression chart
          FutureBuilder<List<ProgressDataPoint>>(
            future: provider.getExerciseProgressOverTime(
              widget.exercise.exerciseTemplateId,
              days: 90,
            ),
            builder: (context, snapshot) {
              if (snapshot.connectionState == ConnectionState.waiting) {
                return const Center(
                  child: Padding(
                    padding: EdgeInsets.all(32),
                    child: CircularProgressIndicator(),
                  ),
                );
              }

              if (snapshot.hasError) {
                return Center(
                  child: Padding(
                    padding: const EdgeInsets.all(32),
                    child: Column(
                      children: [
                        const Icon(
                          Icons.error_outline,
                          size: 48,
                          color: Colors.red,
                        ),
                        const SizedBox(height: 16),
                        Text('Error: ${snapshot.error}'),
                      ],
                    ),
                  ),
                );
              }

              if (!snapshot.hasData || snapshot.data!.isEmpty) {
                return const Center(
                  child: Padding(
                    padding: EdgeInsets.all(32),
                    child: Column(
                      children: [
                        Icon(Icons.show_chart, size: 48, color: Colors.grey),
                        SizedBox(height: 16),
                        Text(
                          'No progress data available',
                          style: TextStyle(color: Colors.grey),
                        ),
                        SizedBox(height: 8),
                        Text(
                          'Perform this exercise a few more times to see your progress!',
                          style: TextStyle(fontSize: 12, color: Colors.grey),
                          textAlign: TextAlign.center,
                        ),
                      ],
                    ),
                  ),
                );
              }

              return ProgressLineChart(
                data: snapshot.data!,
                lineColor: Colors.blue,
                title: 'Max Weight per Session',
              );
            },
          ),

          const SizedBox(height: 24),

          // Personal Record Info
          if (widget.exercise.personalRecord != null &&
              widget.exercise.personalRecordDate != null) ...[
            const Text(
              'Personal Record',
              style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 12),
            Card(
              child: ListTile(
                leading: const Icon(
                  Icons.emoji_events,
                  color: Colors.amber,
                  size: 40,
                ),
                title: Text(
                  '${widget.exercise.personalRecord!.toStringAsFixed(1)} kg',
                  style: const TextStyle(
                    fontSize: 20,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                subtitle: Text(
                  'Achieved on ${_formatDate(widget.exercise.personalRecordDate!)}',
                  style: const TextStyle(fontSize: 12),
                ),
              ),
            ),
          ],
        ],
      ),
    );
  }

  Widget _buildStatsTab() {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'Performance Statistics',
            style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 16),

          _buildStatCard(
            'Times Performed',
            widget.exercise.timesPerformed.toString(),
            'Total number of times you\'ve done this exercise',
            Icons.repeat,
            Colors.blue,
          ),

          _buildStatCard(
            'Total Volume',
            widget.exercise.formattedVolume,
            'Cumulative weight lifted (reps × weight)',
            Icons.line_weight,
            Colors.green,
          ),

          if (widget.exercise.personalRecord != null)
            _buildStatCard(
              'Personal Record',
              '${widget.exercise.personalRecord!.toStringAsFixed(1)} kg',
              'Highest weight lifted in a single set',
              Icons.emoji_events,
              Colors.amber,
            ),

          if (widget.exercise.lastWeight != null)
            _buildStatCard(
              'Last Weight Used',
              '${widget.exercise.lastWeight!.toStringAsFixed(1)} kg',
              'Average weight from most recent session',
              Icons.scale,
              Colors.purple,
            ),

          if (widget.exercise.lastPerformedDate != null)
            _buildStatCard(
              'Last Performed',
              _formatDate(widget.exercise.lastPerformedDate!),
              _getDaysAgo(widget.exercise.lastPerformedDate!),
              Icons.calendar_today,
              Colors.indigo,
            ),

          if (widget.exercise.progressPercentage != null)
            _buildStatCard(
              'Progress',
              widget.exercise.formattedProgress,
              widget.exercise.progressPercentage! >= 0
                  ? 'Improvement since first session'
                  : 'Decrease since first session',
              widget.exercise.progressPercentage! >= 0
                  ? Icons.trending_up
                  : Icons.trending_down,
              widget.exercise.progressPercentage! >= 0
                  ? Colors.green
                  : Colors.red,
            ),
        ],
      ),
    );
  }

  Widget _buildStatCard(
    String title,
    String value,
    String description,
    IconData icon,
    Color color,
  ) {
    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Row(
          children: [
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: color.withValues(alpha: 0.1),
                borderRadius: BorderRadius.circular(8),
              ),
              child: Icon(icon, color: color, size: 28),
            ),
            const SizedBox(width: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    title,
                    style: const TextStyle(fontSize: 12, color: Colors.grey),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    value,
                    style: TextStyle(
                      fontSize: 20,
                      fontWeight: FontWeight.bold,
                      color: color,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    description,
                    style: const TextStyle(fontSize: 11, color: Colors.grey),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  String _formatDate(DateTime date) {
    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);
    final yesterday = today.subtract(const Duration(days: 1));
    final dateOnly = DateTime(date.year, date.month, date.day);

    if (dateOnly == today) return 'Today';
    if (dateOnly == yesterday) return 'Yesterday';

    final months = [
      'Jan',
      'Feb',
      'Mar',
      'Apr',
      'May',
      'Jun',
      'Jul',
      'Aug',
      'Sep',
      'Oct',
      'Nov',
      'Dec',
    ];

    return '${months[date.month - 1]} ${date.day}, ${date.year}';
  }

  String _getDaysAgo(DateTime date) {
    final days = DateTime.now().difference(date).inDays;
    if (days == 0) return 'Today';
    if (days == 1) return '1 day ago';
    if (days < 7) return '$days days ago';
    if (days < 30) return '${(days / 7).floor()} weeks ago';
    return '${(days / 30).floor()} months ago';
  }
}
