import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../../providers/analytics_provider.dart';
import '../../../providers/sessions_provider.dart';
import '../../../providers/goals_provider.dart';
import '../../../data/models/workout_stats.dart';
import '../../widgets/charts/volume_chart.dart';
import '../../widgets/charts/muscle_group_chart.dart';
import '../../widgets/analytics/calendar_heatmap.dart';
import '../../widgets/analytics/streak_counter.dart';
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

    // Load analytics data and goals
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<AnalyticsProvider>().loadAnalytics();
      context.read<GoalsProvider>().loadGoals();
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

    // Get sessions for heatmap and streak counter
    final sessionsProvider = context.watch<SessionsProvider>();
    final sessions = sessionsProvider.sessions;

    return RefreshIndicator(
      onRefresh: () => provider.refresh(),
      child: SingleChildScrollView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Streak Counter
            StreakCounter(sessions: sessions),
            const SizedBox(height: 8),

            // Calendar Heatmap
            CalendarHeatmap(sessions: sessions),
            const SizedBox(height: 16),

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

            // Key Stats (4 most important)
            _buildKeyStatsGrid(stats, provider),

            const SizedBox(height: 16),

            // Active Goals Progress
            _buildActiveGoalsSection(),
          ],
        ),
      ),
    );
  }

  Widget _buildKeyStatsGrid(WorkoutStats stats, AnalyticsProvider provider) {
    return GridView.count(
      crossAxisCount: 2,
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      mainAxisSpacing: 12,
      crossAxisSpacing: 12,
      childAspectRatio: 1.3,
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
          'This Week',
          stats.workoutsThisWeek.toString(),
          Icons.calendar_today,
          Colors.teal,
        ),
        _buildStatCard(
          'Personal Records',
          provider.personalRecords.length.toString(),
          Icons.emoji_events,
          Colors.amber,
        ),
      ],
    );
  }

  Widget _buildActiveGoalsSection() {
    final goalsProvider = context.watch<GoalsProvider>();
    final activeGoals = goalsProvider.activeGoals.take(3).toList();

    if (activeGoals.isEmpty) {
      return const SizedBox.shrink();
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Active Goals',
          style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 12),
        ...activeGoals.map((goal) {
          final progress = goal.progressPercentage / 100;
          return Card(
            margin: const EdgeInsets.only(bottom: 12),
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Expanded(
                        child: Text(
                          goal.goalType,
                          style: const TextStyle(
                            fontSize: 16,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ),
                      Text(
                        '${goal.progressPercentage.toStringAsFixed(0)}%',
                        style: TextStyle(
                          fontSize: 16,
                          fontWeight: FontWeight.bold,
                          color: progress >= 1.0 ? Colors.green : Colors.blue,
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 8),
                  LinearProgressIndicator(
                    value: progress.clamp(0.0, 1.0),
                    backgroundColor: Colors.grey.shade200,
                    valueColor: AlwaysStoppedAnimation<Color>(
                      progress >= 1.0 ? Colors.green : Colors.blue,
                    ),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    '${goal.currentValue.toStringAsFixed(1)} / ${goal.targetValue.toStringAsFixed(1)} ${goal.unit ?? ''}',
                    style: TextStyle(fontSize: 14, color: Colors.grey.shade600),
                  ),
                ],
              ),
            ),
          );
        }),
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

  Widget _buildProgressTab(AnalyticsProvider provider) {
    final muscleGroupData = provider.muscleGroupVolume;

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        // Muscle Group Distribution
        if (muscleGroupData.isNotEmpty) ...[
          const Text(
            'Muscle Group Distribution',
            style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 12),
          MuscleGroupChart(data: muscleGroupData),
          const SizedBox(height: 24),
        ],

        // Exercise Progress Header
        const Text(
          'Exercise Progress',
          style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 12),

        // Exercise Progress List
        if (provider.exerciseProgress.isEmpty)
          const Center(
            child: Padding(
              padding: EdgeInsets.all(32),
              child: Text('No exercise progress data available'),
            ),
          )
        else
          ...provider.exerciseProgress.map((progress) {
            return _buildExerciseProgressCard(progress);
          }),
      ],
    );
  }

  Widget _buildExerciseProgressCard(progress) {
    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      child: ListTile(
        onTap: () {
          Navigator.push(
            context,
            MaterialPageRoute(
              builder: (context) => ExerciseDetailScreen(exercise: progress),
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
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
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
