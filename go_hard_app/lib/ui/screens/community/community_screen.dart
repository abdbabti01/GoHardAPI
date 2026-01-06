import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../../providers/shared_workout_provider.dart';
import '../../../data/models/shared_workout.dart';
import '../../widgets/common/offline_banner.dart';

/// Community screen for browsing and sharing workouts
class CommunityScreen extends StatefulWidget {
  const CommunityScreen({super.key});

  @override
  State<CommunityScreen> createState() => _CommunityScreenState();
}

class _CommunityScreenState extends State<CommunityScreen>
    with SingleTickerProviderStateMixin {
  late TabController _tabController;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 3, vsync: this);

    // Load data
    WidgetsBinding.instance.addPostFrameCallback((_) {
      final provider = context.read<SharedWorkoutProvider>();
      provider.loadSharedWorkouts();
      provider.loadSavedWorkouts();
    });
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final provider = context.watch<SharedWorkoutProvider>();

    return Scaffold(
      appBar: AppBar(
        title: const Text('Community'),
        actions: [
          IconButton(
            icon: const Icon(Icons.filter_list),
            onPressed: _showFilterDialog,
            tooltip: 'Filter',
          ),
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: () => provider.refresh(),
            tooltip: 'Refresh',
          ),
        ],
        bottom: TabBar(
          controller: _tabController,
          tabs: const [
            Tab(text: 'Discover', icon: Icon(Icons.explore)),
            Tab(text: 'Saved', icon: Icon(Icons.bookmark)),
            Tab(text: 'My Shares', icon: Icon(Icons.share)),
          ],
        ),
      ),
      body: Column(
        children: [
          if (!provider.isOnline) const OfflineBanner(),
          Expanded(
            child:
                provider.isLoading
                    ? const Center(child: CircularProgressIndicator())
                    : provider.errorMessage != null
                    ? _buildErrorView(provider)
                    : TabBarView(
                      controller: _tabController,
                      children: [
                        _buildDiscoverTab(provider),
                        _buildSavedTab(provider),
                        _buildMySharesTab(provider),
                      ],
                    ),
          ),
        ],
      ),
    );
  }

  Widget _buildErrorView(SharedWorkoutProvider provider) {
    return Center(
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
    );
  }

  Widget _buildDiscoverTab(SharedWorkoutProvider provider) {
    final workouts = provider.sharedWorkouts;

    if (workouts.isEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.explore_off, size: 64, color: Colors.grey[400]),
            const SizedBox(height: 16),
            Text(
              'No shared workouts found',
              style: TextStyle(fontSize: 16, color: Colors.grey[600]),
            ),
            const SizedBox(height: 8),
            Text(
              'Be the first to share a workout!',
              style: TextStyle(fontSize: 14, color: Colors.grey[500]),
            ),
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: () => provider.loadSharedWorkouts(),
      child: ListView.builder(
        padding: const EdgeInsets.all(16),
        itemCount: workouts.length,
        itemBuilder: (context, index) {
          return _buildWorkoutCard(context, workouts[index], provider);
        },
      ),
    );
  }

  Widget _buildSavedTab(SharedWorkoutProvider provider) {
    final workouts = provider.savedWorkouts;

    if (workouts.isEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.bookmark_border, size: 64, color: Colors.grey[400]),
            const SizedBox(height: 16),
            Text(
              'No saved workouts',
              style: TextStyle(fontSize: 16, color: Colors.grey[600]),
            ),
            const SizedBox(height: 8),
            Text(
              'Save workouts to access them later',
              style: TextStyle(fontSize: 14, color: Colors.grey[500]),
            ),
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: () => provider.loadSavedWorkouts(),
      child: ListView.builder(
        padding: const EdgeInsets.all(16),
        itemCount: workouts.length,
        itemBuilder: (context, index) {
          return _buildWorkoutCard(context, workouts[index], provider);
        },
      ),
    );
  }

  Widget _buildMySharesTab(SharedWorkoutProvider provider) {
    final workouts = provider.mySharedWorkouts;

    if (workouts.isEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.share_outlined, size: 64, color: Colors.grey[400]),
            const SizedBox(height: 16),
            Text(
              'You haven\'t shared any workouts',
              style: TextStyle(fontSize: 16, color: Colors.grey[600]),
            ),
            const SizedBox(height: 8),
            Text(
              'Share your workouts with the community!',
              style: TextStyle(fontSize: 14, color: Colors.grey[500]),
            ),
          ],
        ),
      );
    }

    return ListView.builder(
      padding: const EdgeInsets.all(16),
      itemCount: workouts.length,
      itemBuilder: (context, index) {
        return _buildWorkoutCard(
          context,
          workouts[index],
          provider,
          isMyShare: true,
        );
      },
    );
  }

  Widget _buildWorkoutCard(
    BuildContext context,
    SharedWorkout workout,
    SharedWorkoutProvider provider, {
    bool isMyShare = false,
  }) {
    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      child: InkWell(
        onTap: () => _showWorkoutDetails(workout),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Header
              Row(
                children: [
                  CircleAvatar(
                    child: Text(workout.sharedByUserName[0].toUpperCase()),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          workout.sharedByUserName,
                          style: const TextStyle(fontWeight: FontWeight.bold),
                        ),
                        Text(
                          workout.timeSinceShared,
                          style: TextStyle(
                            fontSize: 12,
                            color: Colors.grey[600],
                          ),
                        ),
                      ],
                    ),
                  ),
                  if (isMyShare)
                    IconButton(
                      icon: const Icon(Icons.delete_outline),
                      onPressed: () => _confirmDelete(workout, provider),
                      color: Colors.red,
                    ),
                ],
              ),
              const SizedBox(height: 12),

              // Workout info
              Text(
                workout.workoutName,
                style: const TextStyle(
                  fontSize: 18,
                  fontWeight: FontWeight.bold,
                ),
              ),
              if (workout.description != null) ...[
                const SizedBox(height: 4),
                Text(
                  workout.description!,
                  style: TextStyle(color: Colors.grey[700]),
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                ),
              ],
              const SizedBox(height: 8),

              // Tags
              Wrap(
                spacing: 8,
                runSpacing: 4,
                children: [
                  Chip(
                    label: Text(workout.category),
                    visualDensity: VisualDensity.compact,
                  ),
                  if (workout.difficulty != null)
                    Chip(
                      label: Text(workout.difficulty!),
                      visualDensity: VisualDensity.compact,
                    ),
                  Chip(
                    label: Text(workout.formattedDuration),
                    visualDensity: VisualDensity.compact,
                    avatar: const Icon(Icons.timer, size: 16),
                  ),
                ],
              ),
              const SizedBox(height: 12),

              // Actions
              Row(
                children: [
                  IconButton(
                    icon: Icon(
                      workout.isLikedByCurrentUser
                          ? Icons.favorite
                          : Icons.favorite_border,
                    ),
                    color: workout.isLikedByCurrentUser ? Colors.red : null,
                    onPressed: () => provider.toggleLike(workout.id),
                  ),
                  Text('${workout.likeCount}'),
                  const SizedBox(width: 16),
                  IconButton(
                    icon: Icon(
                      workout.isSavedByCurrentUser
                          ? Icons.bookmark
                          : Icons.bookmark_border,
                    ),
                    color: workout.isSavedByCurrentUser ? Colors.blue : null,
                    onPressed: () => provider.toggleSave(workout.id),
                  ),
                  Text('${workout.saveCount}'),
                  const Spacer(),
                  ElevatedButton.icon(
                    onPressed: () => _useWorkout(workout),
                    icon: const Icon(Icons.play_arrow, size: 18),
                    label: const Text('Use'),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }

  void _showWorkoutDetails(SharedWorkout workout) {
    showDialog(
      context: context,
      builder:
          (context) => AlertDialog(
            title: Text(workout.workoutName),
            content: SingleChildScrollView(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text('Shared by: ${workout.sharedByUserName}'),
                  const SizedBox(height: 8),
                  Text('Category: ${workout.category}'),
                  if (workout.difficulty != null)
                    Text('Difficulty: ${workout.difficulty}'),
                  Text('Duration: ${workout.formattedDuration}'),
                  const SizedBox(height: 16),
                  if (workout.description != null) ...[
                    const Text(
                      'Description:',
                      style: TextStyle(fontWeight: FontWeight.bold),
                    ),
                    const SizedBox(height: 4),
                    Text(workout.description!),
                    const SizedBox(height: 16),
                  ],
                  const Text(
                    'Exercises:',
                    style: TextStyle(fontWeight: FontWeight.bold),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    workout.exercisesJson,
                  ), // TODO: Parse and display properly
                ],
              ),
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(context),
                child: const Text('Close'),
              ),
              ElevatedButton(
                onPressed: () {
                  Navigator.pop(context);
                  _useWorkout(workout);
                },
                child: const Text('Use Workout'),
              ),
            ],
          ),
    );
  }

  void _useWorkout(SharedWorkout workout) {
    showDialog(
      context: context,
      builder:
          (context) => AlertDialog(
            title: const Text('Use Workout'),
            content: Text(
              'Create a new workout session from "${workout.workoutName}"?\n\n'
              'This will start a new workout with the exercises from this shared workout.',
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(context),
                child: const Text('Cancel'),
              ),
              ElevatedButton(
                onPressed: () {
                  Navigator.pop(context);
                  _createSessionFromSharedWorkout(workout);
                },
                child: const Text('Start Workout'),
              ),
            ],
          ),
    );
  }

  Future<void> _createSessionFromSharedWorkout(SharedWorkout workout) async {
    final messenger = ScaffoldMessenger.of(context);

    // TODO: Integrate with SessionsProvider to create actual session
    // For now, show success message
    messenger.showSnackBar(
      SnackBar(
        content: Text(
          'Workout session created from "${workout.workoutName}"!\n'
          'Navigate to Active Workout to start.',
        ),
        duration: const Duration(seconds: 3),
        action: SnackBarAction(
          label: 'GO',
          onPressed: () {
            // Navigate to active workout screen
            Navigator.pushNamed(context, '/active-workout');
          },
        ),
      ),
    );
  }

  void _confirmDelete(SharedWorkout workout, SharedWorkoutProvider provider) {
    showDialog(
      context: context,
      builder:
          (context) => AlertDialog(
            title: const Text('Delete Shared Workout'),
            content: Text(
              'Are you sure you want to delete "${workout.workoutName}"?',
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(context),
                child: const Text('Cancel'),
              ),
              ElevatedButton(
                onPressed: () async {
                  Navigator.pop(context);
                  final messenger = ScaffoldMessenger.of(context);
                  final success = await provider.deleteSharedWorkout(
                    workout.id,
                  );
                  if (success && mounted) {
                    messenger.showSnackBar(
                      const SnackBar(content: Text('Workout deleted')),
                    );
                  }
                },
                style: ElevatedButton.styleFrom(backgroundColor: Colors.red),
                child: const Text('Delete'),
              ),
            ],
          ),
    );
  }

  void _showFilterDialog() {
    final provider = context.read<SharedWorkoutProvider>();

    showDialog(
      context: context,
      builder:
          (context) => AlertDialog(
            title: const Text('Filter Workouts'),
            content: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text('Category:'),
                DropdownButton<String?>(
                  value: provider.selectedCategory,
                  isExpanded: true,
                  items: [
                    const DropdownMenuItem(value: null, child: Text('All')),
                    ...[
                      'Strength',
                      'Cardio',
                      'Flexibility',
                      'HIIT',
                      'CrossFit',
                    ].map(
                      (cat) => DropdownMenuItem(value: cat, child: Text(cat)),
                    ),
                  ],
                  onChanged: (value) {
                    provider.setCategory(value);
                    Navigator.pop(context);
                  },
                ),
                const SizedBox(height: 16),
                const Text('Difficulty:'),
                DropdownButton<String?>(
                  value: provider.selectedDifficulty,
                  isExpanded: true,
                  items: [
                    const DropdownMenuItem(value: null, child: Text('All')),
                    ...['Beginner', 'Intermediate', 'Advanced'].map(
                      (diff) =>
                          DropdownMenuItem(value: diff, child: Text(diff)),
                    ),
                  ],
                  onChanged: (value) {
                    provider.setDifficulty(value);
                    Navigator.pop(context);
                  },
                ),
              ],
            ),
            actions: [
              TextButton(
                onPressed: () {
                  provider.clearFilters();
                  Navigator.pop(context);
                },
                child: const Text('Clear Filters'),
              ),
              TextButton(
                onPressed: () => Navigator.pop(context),
                child: const Text('Close'),
              ),
            ],
          ),
    );
  }
}
