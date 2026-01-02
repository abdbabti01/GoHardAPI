# Analytics & Progress Tracking - Implementation Guide

> **Complete guide to finish implementing analytics and progress tracking features**

---

## Overview

Analytics features have been partially implemented. This guide explains what's done and what remains.

### ✅ Completed

1. **API Models** (GoHardAPI/Models/)
   - `WorkoutStats.cs` - Overall workout statistics
   - `ExerciseProgress.cs` - Exercise-specific progress
   - `ProgressDataPoint.cs` - Time-series data, muscle group volume, personal records

2. **API Controller** (GoHardAPI/Controllers/AnalyticsController.cs)
   - `GET /api/analytics/stats` - Overall workout statistics
   - `GET /api/analytics/exercise-progress` - All exercises progress
   - `GET /api/analytics/exercise-progress/{id}` - Specific exercise over time
   - `GET /api/analytics/muscle-group-volume` - Volume distribution
   - `GET /api/analytics/personal-records` - All PRs
   - `GET /api/analytics/volume-over-time` - Workout volume trend

3. **Flutter Dependencies**
   - `fl_chart: ^0.68.0` added to pubspec.yaml

4. **Flutter Models** (go_hard_app/lib/data/models/workout_stats.dart)
   - `WorkoutStats` - Overall statistics
   - `ExerciseProgress` - Exercise progress
   - `ProgressDataPoint` - Chart data points
   - `MuscleGroupVolume` - Muscle group breakdown
   - `PersonalRecord` - PR entries

---

## 🔨 Remaining Implementation

### Step 1: Create Analytics Repository

**File:** `go_hard_app/lib/data/repositories/analytics_repository.dart`

```dart
import '../services/api_service.dart';
import '../models/workout_stats.dart';
import '../../core/constants/api_config.dart';

class AnalyticsRepository {
  final ApiService _apiService;

  AnalyticsRepository(this._apiService);

  /// Get overall workout statistics
  Future<WorkoutStats> getWorkoutStats() async {
    final data = await _apiService.get<Map<String, dynamic>>('analytics/stats');
    return WorkoutStats.fromJson(data);
  }

  /// Get progress for all exercises
  Future<List<ExerciseProgress>> getExerciseProgress() async {
    final data = await _apiService.get<List<dynamic>>('analytics/exercise-progress');
    return data.map((json) => ExerciseProgress.fromJson(json as Map<String, dynamic>)).toList();
  }

  /// Get progress over time for specific exercise
  Future<List<ProgressDataPoint>> getExerciseProgressOverTime(
    int exerciseTemplateId, {
    int days = 90,
  }) async {
    final data = await _apiService.get<List<dynamic>>(
      'analytics/exercise-progress/$exerciseTemplateId?days=$days',
    );
    return data.map((json) => ProgressDataPoint.fromJson(json as Map<String, dynamic>)).toList();
  }

  /// Get muscle group volume distribution
  Future<List<MuscleGroupVolume>> getMuscleGroupVolume({int days = 30}) async {
    final data = await _apiService.get<List<dynamic>>(
      'analytics/muscle-group-volume?days=$days',
    );
    return data.map((json) => MuscleGroupVolume.fromJson(json as Map<String, dynamic>)).toList();
  }

  /// Get all personal records
  Future<List<PersonalRecord>> getPersonalRecords() async {
    final data = await _apiService.get<List<dynamic>>('analytics/personal-records');
    return data.map((json) => PersonalRecord.fromJson(json as Map<String, dynamic>)).toList();
  }

  /// Get volume over time
  Future<List<ProgressDataPoint>> getVolumeOverTime({int days = 90}) async {
    final data = await _apiService.get<List<dynamic>>(
      'analytics/volume-over-time?days=$days',
    );
    return data.map((json) => ProgressDataPoint.fromJson(json as Map<String, dynamic>)).toList();
  }
}
```

---

### Step 2: Create Analytics Provider

**File:** `go_hard_app/lib/providers/analytics_provider.dart`

```dart
import 'package:flutter/foundation.dart';
import '../data/repositories/analytics_repository.dart';
import '../data/models/workout_stats.dart';

class AnalyticsProvider extends ChangeNotifier {
  final AnalyticsRepository _repository;

  WorkoutStats? _workoutStats;
  List<ExerciseProgress> _exerciseProgress = [];
  List<PersonalRecord> _personalRecords = [];
  List<MuscleGroupVolume> _muscleGroupVolume = [];

  bool _isLoading = false;
  String? _errorMessage;

  AnalyticsProvider(this._repository);

  // Getters
  WorkoutStats? get workoutStats => _workoutStats;
  List<ExerciseProgress> get exerciseProgress => _exerciseProgress;
  List<PersonalRecord> get personalRecords => _personalRecords;
  List<MuscleGroupVolume> get muscleGroupVolume => _muscleGroupVolume;
  bool get isLoading => _isLoading;
  String? get errorMessage => _errorMessage;

  /// Load all analytics data
  Future<void> loadAnalytics() async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      // Load all data in parallel
      final results = await Future.wait([
        _repository.getWorkoutStats(),
        _repository.getExerciseProgress(),
        _repository.getPersonalRecords(),
        _repository.getMuscleGroupVolume(days: 30),
      ]);

      _workoutStats = results[0] as WorkoutStats;
      _exerciseProgress = results[1] as List<ExerciseProgress>;
      _personalRecords = results[2] as List<PersonalRecord>;
      _muscleGroupVolume = results[3] as List<MuscleGroupVolume>;
    } catch (e) {
      _errorMessage = 'Failed to load analytics: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Analytics error: $e');
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  /// Get progress over time for specific exercise
  Future<List<ProgressDataPoint>> getExerciseProgressOverTime(
    int exerciseTemplateId, {
    int days = 90,
  }) async {
    try {
      return await _repository.getExerciseProgressOverTime(exerciseTemplateId, days: days);
    } catch (e) {
      debugPrint('Error loading exercise progress: $e');
      return [];
    }
  }

  /// Get volume over time
  Future<List<ProgressDataPoint>> getVolumeOverTime({int days = 90}) async {
    try {
      return await _repository.getVolumeOverTime(days: days);
    } catch (e) {
      debugPrint('Error loading volume over time: $e');
      return [];
    }
  }

  /// Refresh analytics data
  Future<void> refresh() async {
    await loadAnalytics();
  }

  void clearError() {
    _errorMessage = null;
    notifyListeners();
  }
}
```

---

### Step 3: Add to Dependency Injection

**Update:** `go_hard_app/lib/main.dart`

Add to providers list:

```dart
// Analytics Repository
ProxyProvider<ApiService, AnalyticsRepository>(
  update: (_, apiService, __) => AnalyticsRepository(apiService),
),

// Analytics Provider
ChangeNotifierProxyProvider<AnalyticsRepository, AnalyticsProvider>(
  create: (context) => AnalyticsProvider(context.read<AnalyticsRepository>()),
  update: (_, analyticsRepo, previous) => previous ?? AnalyticsProvider(analyticsRepo),
),
```

---

### Step 4: Create Analytics Screen

**File:** `go_hard_app/lib/ui/screens/analytics/analytics_screen.dart`

```dart
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:fl_chart/fl_chart.dart';
import '../../../providers/analytics_provider.dart';

class AnalyticsScreen extends StatefulWidget {
  const AnalyticsScreen({super.key});

  @override
  State<AnalyticsScreen> createState() => _AnalyticsScreenState();
}

class _AnalyticsScreenState extends State<AnalyticsScreen> with SingleTickerProviderStateMixin {
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
      body: provider.isLoading
          ? const Center(child: CircularProgressIndicator())
          : provider.errorMessage != null
              ? Center(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      const Icon(Icons.error_outline, size: 64, color: Colors.red),
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

    return RefreshIndicator(
      onRefresh: () => provider.refresh(),
      child: SingleChildScrollView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Stats Grid
            _buildStatsGrid(stats),

            const SizedBox(height: 24),

            // Muscle Group Distribution
            const Text(
              'Volume by Muscle Group',
              style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 16),
            _buildMuscleGroupChart(provider.muscleGroupVolume),

            const SizedBox(height: 24),

            // Volume Over Time
            const Text(
              'Volume Over Time',
              style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 16),
            FutureBuilder<List<ProgressDataPoint>>(
              future: provider.getVolumeOverTime(days: 30),
              builder: (context, snapshot) {
                if (!snapshot.hasData) {
                  return const Center(child: CircularProgressIndicator());
                }
                return _buildLineChart(snapshot.data!);
              },
            ),
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
      childAspectRatio: 1.5,
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

  Widget _buildStatCard(String title, String value, IconData icon, Color color) {
    return Card(
      elevation: 2,
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(icon, size: 32, color: color),
            const SizedBox(height: 8),
            Text(
              value,
              style: TextStyle(
                fontSize: 24,
                fontWeight: FontWeight.bold,
                color: color,
              ),
            ),
            const SizedBox(height: 4),
            Text(
              title,
              style: const TextStyle(fontSize: 12, color: Colors.grey),
              textAlign: TextAlign.center,
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildMuscleGroupChart(List<MuscleGroupVolume> data) {
    if (data.isEmpty) {
      return const Center(child: Text('No data available'));
    }

    return SizedBox(
      height: 200,
      child: PieChart(
        PieChartData(
          sections: data.map((muscle) {
            return PieChartSectionData(
              value: muscle.volume,
              title: '${muscle.percentage.toStringAsFixed(0)}%',
              color: _getColorForMuscleGroup(muscle.muscleGroup),
              radius: 80,
              titleStyle: const TextStyle(
                fontSize: 14,
                fontWeight: FontWeight.bold,
                color: Colors.white,
              ),
            );
          }).toList(),
          sectionsSpace: 2,
          centerSpaceRadius: 40,
        ),
      ),
    );
  }

  Widget _buildLineChart(List<ProgressDataPoint> data) {
    if (data.isEmpty) {
      return const Center(child: Text('No data available'));
    }

    return SizedBox(
      height: 200,
      child: LineChart(
        LineChartData(
          gridData: const FlGridData(show: true),
          titlesData: const FlTitlesData(
            leftTitles: AxisTitles(sideTitles: SideTitles(showTitles: true)),
            bottomTitles: AxisTitles(sideTitles: SideTitles(showTitles: false)),
            topTitles: AxisTitles(sideTitles: SideTitles(showTitles: false)),
            rightTitles: AxisTitles(sideTitles: SideTitles(showTitles: false)),
          ),
          borderData: FlBorderData(show: true),
          lineBarsData: [
            LineChartBarData(
              spots: data.asMap().entries.map((entry) {
                return FlSpot(entry.key.toDouble(), entry.value.value);
              }).toList(),
              isCurved: true,
              color: Colors.blue,
              barWidth: 3,
              dotData: const FlDotData(show: true),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildProgressTab(AnalyticsProvider provider) {
    return ListView.builder(
      padding: const EdgeInsets.all(16),
      itemCount: provider.exerciseProgress.length,
      itemBuilder: (context, index) {
        final progress = provider.exerciseProgress[index];
        return Card(
          margin: const EdgeInsets.only(bottom: 12),
          child: ListTile(
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
            trailing: progress.progressPercentage != null
                ? Container(
                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                    decoration: BoxDecoration(
                      color: (progress.progressPercentage! >= 0)
                          ? Colors.green.withOpacity(0.2)
                          : Colors.red.withOpacity(0.2),
                      borderRadius: BorderRadius.circular(12),
                    ),
                    child: Text(
                      progress.formattedProgress,
                      style: TextStyle(
                        color: (progress.progressPercentage! >= 0) ? Colors.green : Colors.red,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  )
                : null,
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
            leading: const Icon(Icons.emoji_events, color: Colors.amber, size: 40),
            title: Text(pr.exerciseName, style: const TextStyle(fontWeight: FontWeight.bold)),
            subtitle: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('PR: ${pr.formattedPR}'),
                Text('Est. 1RM: ${pr.formattedOneRM}'),
                Text('Achieved: ${pr.timeSincePR}', style: const TextStyle(fontSize: 12)),
              ],
            ),
          ),
        );
      },
    );
  }

  Color _getColorForMuscleGroup(String muscleGroup) {
    const colors = {
      'Chest': Colors.blue,
      'Back': Colors.green,
      'Legs': Colors.orange,
      'Shoulders': Colors.purple,
      'Arms': Colors.red,
      'Core': Colors.teal,
    };
    return colors[muscleGroup] ?? Colors.grey;
  }
}
```

---

### Step 5: Add Route

**Update:** `go_hard_app/lib/app.dart`

Add to routes:

```dart
'/analytics': (context) => const AnalyticsScreen(),
```

---

### Step 6: Add Navigation

**Update:** `go_hard_app/lib/ui/screens/sessions/sessions_screen.dart`

Add button to AppBar:

```dart
actions: [
  IconButton(
    icon: const Icon(Icons.analytics),
    onPressed: () {
      Navigator.pushNamed(context, '/analytics');
    },
    tooltip: 'Analytics',
  ),
],
```

---

## Testing Steps

### 1. Test API Endpoints

**Using Postman or cURL:**

```bash
# Get workout stats
curl -X GET http://localhost:5000/api/analytics/stats \
  -H "Authorization: Bearer YOUR_TOKEN"

# Get exercise progress
curl -X GET http://localhost:5000/api/analytics/exercise-progress \
  -H "Authorization: Bearer YOUR_TOKEN"

# Get personal records
curl -X GET http://localhost:5000/api/analytics/personal-records \
  -H "Authorization: Bearer YOUR_TOKEN"

# Get muscle group volume
curl -X GET "http://localhost:5000/api/analytics/muscle-group-volume?days=30" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### 2. Test Mobile App

1. **Build and run:**
```bash
cd go_hard_app
flutter pub get
dart run build_runner build --delete-conflicting-outputs
flutter run
```

2. **Create test data:**
   - Create 5-10 workouts
   - Add various exercises (different muscle groups)
   - Log sets with different weights
   - Complete workouts

3. **Test analytics screen:**
   - Tap Analytics icon
   - Verify stats display correctly
   - Check charts render properly
   - Verify PR calculations
   - Test pull-to-refresh

---

## Additional Enhancements (Optional)

### 1. Calendar Heatmap

**Package:** `flutter_heatmap_calendar: ^1.0.5`

Shows workout consistency like GitHub contributions.

### 2. Body Measurements Tracking

**New Model:**
```csharp
public class BodyMeasurement {
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime Date { get; set; }
    public double? Weight { get; set; }
    public double? BodyFat { get; set; }
    public double? Chest { get; set; }
    public double? Waist { get; set; }
    public double? Arms { get; set; }
    public double? Legs { get; set; }
}
```

### 3. Export Data

**Feature:** Export workout history to CSV/PDF

```dart
Future<void> exportToCsv() async {
  final stats = await _repository.getWorkoutStats();
  final csv = 'Date,Duration,Volume\n...';
  // Save to file
}
```

### 4. Goal Setting

**Feature:** Set and track workout goals

```csharp
public class WorkoutGoal {
    public int Id { get; set; }
    public int UserId { get; set; }
    public string GoalType { get; set; }  // "frequency", "volume", "pr"
    public int TargetValue { get; set; }
    public DateTime TargetDate { get; set; }
    public double Progress { get; set; }
}
```

### 5. Comparison View

**Feature:** Compare current period vs. previous period

```dart
class PeriodComparison {
  final WorkoutStats currentPeriod;
  final WorkoutStats previousPeriod;
  final double changePercentage;
}
```

---

## Troubleshooting

### Issue: Charts not rendering

**Solution:**
- Ensure `fl_chart` package installed
- Run `flutter pub get`
- Check data is not empty
- Verify chart dimensions (height/width)

### Issue: API returns 401 Unauthorized

**Solution:**
- Check JWT token is valid
- Verify `Authorization: Bearer <token>` header
- Token might be expired (login again)

### Issue: No data in analytics

**Solution:**
- Create and complete some workouts first
- Verify sessions have `status = "completed"`
- Check exercises have sets with weight/reps
- Run at least 3-5 workouts for meaningful data

### Issue: Build runner fails

**Solution:**
```bash
flutter clean
flutter pub get
dart run build_runner clean
dart run build_runner build --delete-conflicting-outputs
```

---

## Next Steps

1. **Complete the implementation** following steps 1-6 above
2. **Test thoroughly** with real workout data
3. **Add offline caching** for analytics (optional)
4. **Implement push notifications** for PR achievements (optional)
5. **Add social sharing** for PR/progress (optional)

---

## Summary

**What's Done:**
- ✅ API endpoints (6 endpoints)
- ✅ API models (5 models)
- ✅ Flutter models (5 models)
- ✅ Charts package added

**What Remains:**
- ⏳ Analytics repository (copy code above)
- ⏳ Analytics provider (copy code above)
- ⏳ Analytics screen UI (copy code above)
- ⏳ Add to dependency injection
- ⏳ Add navigation
- ⏳ Test and refine

**Estimated Time:** 2-3 hours to complete remaining steps

---

**Good luck! 🚀**
