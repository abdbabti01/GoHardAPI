import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../../providers/analytics_provider.dart';
import '../../../data/models/workout_stats.dart';
import '../../widgets/charts/volume_chart.dart';
import '../../widgets/charts/muscle_group_chart.dart';
import 'exercise_detail_screen.dart';

class AnalyticsScreen extends StatefulWidget {
  const AnalyticsScreen({super.key});

  @override
  State<AnalyticsScreen> createState() => _AnalyticsScreenState();
}

class _AnalyticsScreenState extends State<AnalyticsScreen>
    with SingleTickerProviderStateMixin {
  late TabController _tabController;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 3, vsync: this);

    // Load analytics data
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<AnalyticsProvider>().loadAnalytics();
    });
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
        title: const Text('Analytics'),
        bottom: TabBar(
          controller: _tabController,
          tabs: const [
            Tab(text: 'Overview', icon: Icon(Icons.dashboard)),
            Tab(text: 'Progress', icon: Icon(Icons.trending_up)),
            Tab(text: 'Records', icon: Icon(Icons.emoji_events)),
          ],
        ),
      ),
      body:
          provider.isLoading
              ? const Center(child: CircularProgressIndicator())
              : provider.errorMessage != null
              ? Center(
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    const Icon(
                      Icons.error_outline,
                      size: 64,
                      color: Colors.red,
                    ),
                    const SizedBox(height: 16),
                    Text(provider.errorMessage!),
                    const SizedBox(height: 16),
                    ElevatedButton.icon(
                      onPressed: () => provider.refresh(),
                      icon: const Icon(Icons.refresh),
                      label: const Text('Retry'),
                    ),
                  ],
                ),
              )
              : TabBarView(
                controller: _tabController,
                children: [
                  _buildOverviewTab(provider),
                  _buildProgressTab(provider),
                  _buildRecordsTab(provider),
                ],
              ),
    );
  }

  Widget _buildOverviewTab(AnalyticsProvider provider) {
    final stats = provider.workoutStats;
    if (stats == null) {
      return const Center(child: Text('No data available'));
    }

    // Get muscle group data
    final muscleGroupData = provider.muscleGroupVolume;

    return RefreshIndicator(
      onRefresh: () => provider.refresh(),
      child: SingleChildScrollView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Volume Over Time Chart
            FutureBuilder<List<ProgressDataPoint>>(
              future: provider.getVolumeOverTime(days: 30),
              builder: (context, snapshot) {
                if (snapshot.hasData && snapshot.data!.isNotEmpty) {
                  return Column(
                    children: [
                      VolumeChart(data: snapshot.data!, lineColor: Colors.blue),
                      const SizedBox(height: 16),
                    ],
                  );
                }
                return const SizedBox.shrink();
              },
            ),

            // Muscle Group Distribution Chart
            if (muscleGroupData.isNotEmpty) ...[
              MuscleGroupChart(data: muscleGroupData),
              const SizedBox(height: 16),
            ],

            // Stats Grid
            _buildStatsGrid(stats),

            const SizedBox(height: 16),

            // Additional Stats
            _buildAdditionalStats(stats, provider),
          ],
        ),
      ),
    );
  }

  Widget _buildStatsGrid(WorkoutStats stats) {
    return GridView.count(
      crossAxisCount: 2,
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      mainAxisSpacing: 12,
      crossAxisSpacing: 12,
      childAspectRatio: 1.1,
      children: [
        _buildStatCard(
          'Total Workouts',
          stats.totalWorkouts.toString(),
          Icons.fitness_center,
          Colors.blue,
        ),
        _buildStatCard(
          'Current Streak',
          '${stats.currentStreak} days',
          Icons.local_fire_department,
          Colors.orange,
        ),
        _buildStatCard(
          'Total Duration',
          stats.formattedTotalDuration,
          Icons.timer,
          Colors.green,
        ),
        _buildStatCard(
          'Total Volume',
          stats.formattedTotalVolume,
          Icons.fitness_center,
          Colors.purple,
        ),
        _buildStatCard(
          'This Week',
          stats.workoutsThisWeek.toString(),
          Icons.calendar_today,
          Colors.teal,
        ),
        _buildStatCard(
          'Average Duration',
          stats.formattedAverageDuration,
          Icons.access_time,
          Colors.indigo,
        ),
      ],
    );
  }

  Widget _buildStatCard(
    String title,
    String value,
    IconData icon,
    Color color,
  ) {
    return Card(
      elevation: 2,
      child: Padding(
        padding: const EdgeInsets.all(8),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(icon, size: 28, color: color),
            const SizedBox(height: 6),
            Text(
              value,
              style: TextStyle(
                fontSize: 22,
                fontWeight: FontWeight.bold,
                color: color,
              ),
            ),
            const SizedBox(height: 2),
            Text(
              title,
              style: const TextStyle(fontSize: 11, color: Colors.grey),
              textAlign: TextAlign.center,
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildAdditionalStats(WorkoutStats stats, AnalyticsProvider provider) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Additional Stats',
          style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 12),
        GridView.count(
          crossAxisCount: 2,
          shrinkWrap: true,
          physics: const NeverScrollableScrollPhysics(),
          mainAxisSpacing: 12,
          crossAxisSpacing: 12,
          childAspectRatio: 1.1,
          children: [
            _buildStatCard(
              'Longest Streak',
              '${stats.longestStreak} days',
              Icons.emoji_events,
              Colors.amber,
            ),
            _buildStatCard(
              'This Month',
              stats.workoutsThisMonth.toString(),
              Icons.calendar_month,
              Colors.deepPurple,
            ),
            _buildStatCard(
              'Total Reps',
              stats.totalReps.toString(),
              Icons.repeat,
              Colors.cyan,
            ),
            _buildStatCard(
              'Total Sets',
              stats.totalSets.toString(),
              Icons.fitness_center,
              Colors.pink,
            ),
            _buildStatCard(
              'Exercises',
              provider.exerciseProgress.length.toString(),
              Icons.list,
              Colors.deepOrange,
            ),
            _buildStatCard(
              'PRs',
              provider.personalRecords.length.toString(),
              Icons.military_tech,
              Colors.yellow.shade700,
            ),
          ],
        ),
      ],
    );
  }

  Widget _buildProgressTab(AnalyticsProvider provider) {
    if (provider.exerciseProgress.isEmpty) {
      return const Center(child: Text('No exercise progress data available'));
    }

    return ListView.builder(
      padding: const EdgeInsets.all(16),
      itemCount: provider.exerciseProgress.length,
      itemBuilder: (context, index) {
        final progress = provider.exerciseProgress[index];
        return Card(
          margin: const EdgeInsets.only(bottom: 12),
          child: ListTile(
            onTap: () {
              Navigator.push(
                context,
                MaterialPageRoute(
                  builder:
                      (context) => ExerciseDetailScreen(exercise: progress),
                ),
              );
            },
            title: Text(progress.exerciseName),
            subtitle: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('Performed: ${progress.timesPerformed} times'),
                Text('Volume: ${progress.formattedVolume}'),
                if (progress.personalRecord != null)
                  Text('PR: ${progress.personalRecord!.toStringAsFixed(1)} kg'),
              ],
            ),
            trailing: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                if (progress.progressPercentage != null)
                  Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 8,
                      vertical: 4,
                    ),
                    decoration: BoxDecoration(
                      color:
                          (progress.progressPercentage! >= 0)
                              ? Colors.green.withValues(alpha: 0.2)
                              : Colors.red.withValues(alpha: 0.2),
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: Text(
                      progress.formattedProgress,
                      style: TextStyle(
                        color:
                            (progress.progressPercentage! >= 0)
                                ? Colors.green
                                : Colors.red,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ),
                const SizedBox(width: 8),
                const Icon(Icons.chevron_right, color: Colors.grey),
              ],
            ),
          ),
        );
      },
    );
  }

  Widget _buildRecordsTab(AnalyticsProvider provider) {
    if (provider.personalRecords.isEmpty) {
      return const Center(child: Text('No personal records yet'));
    }

    return ListView.builder(
      padding: const EdgeInsets.all(16),
      itemCount: provider.personalRecords.length,
      itemBuilder: (context, index) {
        final pr = provider.personalRecords[index];
        return Card(
          margin: const EdgeInsets.only(bottom: 12),
          child: ListTile(
            leading: const Icon(
              Icons.emoji_events,
              color: Colors.amber,
              size: 40,
            ),
            title: Text(
              pr.exerciseName,
              style: const TextStyle(fontWeight: FontWeight.bold),
            ),
            subtitle: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('PR: ${pr.formattedPR}'),
                Text('Est. 1RM: ${pr.formattedOneRM}'),
                Text(
                  'Achieved: ${pr.timeSincePR}',
                  style: const TextStyle(fontSize: 12),
                ),
              ],
            ),
          ),
        );
      },
    );
  }
}
