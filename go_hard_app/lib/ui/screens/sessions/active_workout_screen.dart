import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../../providers/active_workout_provider.dart';
import '../../../routes/route_names.dart';

/// Active workout screen with timer and exercise management
/// Matches ActiveWorkoutPage.xaml from MAUI app
class ActiveWorkoutScreen extends StatefulWidget {
  final int sessionId;

  const ActiveWorkoutScreen({super.key, required this.sessionId});

  @override
  State<ActiveWorkoutScreen> createState() => _ActiveWorkoutScreenState();
}

class _ActiveWorkoutScreenState extends State<ActiveWorkoutScreen> {
  @override
  void initState() {
    super.initState();
    // Load session and start timer
    WidgetsBinding.instance.addPostFrameCallback((_) {
      final provider = context.read<ActiveWorkoutProvider>();
      provider.loadSession(widget.sessionId);

      // Start workout if it's still a draft
      if (provider.currentSession?.status == 'draft') {
        provider.startWorkout();
      }
    });
  }

  Future<void> _handleAddExercise() async {
    final result = await Navigator.of(
      context,
    ).pushNamed(RouteNames.addExercise, arguments: widget.sessionId);

    // Reload session if exercise was added
    if (result == true && mounted) {
      context.read<ActiveWorkoutProvider>().loadSession(widget.sessionId);
    }
  }

  Future<void> _handleFinishWorkout() async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder:
          (context) => AlertDialog(
            title: const Text('Finish Workout'),
            content: const Text('Are you ready to finish this workout?'),
            actions: [
              TextButton(
                onPressed: () => Navigator.of(context).pop(false),
                child: const Text('Cancel'),
              ),
              ElevatedButton(
                onPressed: () => Navigator.of(context).pop(true),
                child: const Text('Finish'),
              ),
            ],
          ),
    );

    if (confirmed == true && mounted) {
      final provider = context.read<ActiveWorkoutProvider>();
      final success = await provider.finishWorkout();

      if (success && mounted) {
        // Pop back to sessions screen
        Navigator.of(context).pop();
      }
    }
  }

  void _handleExerciseTap(int exerciseId) {
    Navigator.of(context).pushNamed(RouteNames.logSets, arguments: exerciseId);
  }

  @override
  Widget build(BuildContext context) {
    return Consumer<ActiveWorkoutProvider>(
      builder: (context, provider, child) {
        return PopScope(
          canPop: false,
          onPopInvokedWithResult: (bool didPop, dynamic result) async {
            if (didPop) return;

            // Warn user about leaving active workout
            final shouldPop = await showDialog<bool>(
              context: context,
              builder:
                  (context) => AlertDialog(
                    title: const Text('Leave Workout'),
                    content: const Text(
                      'Your workout is still in progress. Are you sure you want to leave?',
                    ),
                    actions: [
                      TextButton(
                        onPressed: () => Navigator.of(context).pop(false),
                        child: const Text('Stay'),
                      ),
                      TextButton(
                        onPressed: () => Navigator.of(context).pop(true),
                        style: TextButton.styleFrom(
                          foregroundColor: Colors.red,
                        ),
                        child: const Text('Leave'),
                      ),
                    ],
                  ),
            );

            if (shouldPop == true && context.mounted) {
              Navigator.of(context).pop();
            }
          },
          child: Scaffold(
            appBar: AppBar(
              title: const Text('Active Workout'),
              actions: [
                IconButton(
                  icon: const Icon(Icons.check_circle),
                  onPressed: _handleFinishWorkout,
                  tooltip: 'Finish Workout',
                ),
              ],
            ),
            body: _buildBody(provider),
            floatingActionButton: FloatingActionButton.extended(
              onPressed: _handleAddExercise,
              icon: const Icon(Icons.add),
              label: const Text('Add Exercise'),
            ),
          ),
        );
      },
    );
  }

  Widget _buildBody(ActiveWorkoutProvider provider) {
    if (provider.isLoading) {
      return const Center(child: CircularProgressIndicator());
    }

    if (provider.errorMessage != null && provider.errorMessage!.isNotEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.error_outline, size: 64, color: Colors.red.shade300),
            const SizedBox(height: 16),
            Text('Error', style: Theme.of(context).textTheme.titleLarge),
            const SizedBox(height: 8),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 32),
              child: Text(
                provider.errorMessage!,
                textAlign: TextAlign.center,
                style: Theme.of(
                  context,
                ).textTheme.bodyMedium?.copyWith(color: Colors.grey),
              ),
            ),
          ],
        ),
      );
    }

    return Column(
      children: [
        // Timer card
        _buildTimerCard(provider),

        // Exercises list
        Expanded(
          child:
              provider.exercises.isEmpty
                  ? _buildEmptyState()
                  : _buildExercisesList(provider),
        ),
      ],
    );
  }

  Widget _buildTimerCard(ActiveWorkoutProvider provider) {
    return Card(
      margin: const EdgeInsets.all(16),
      elevation: 4,
      child: Container(
        width: double.infinity,
        padding: const EdgeInsets.symmetric(vertical: 24, horizontal: 16),
        decoration: BoxDecoration(
          borderRadius: BorderRadius.circular(12),
          gradient: LinearGradient(
            colors: [
              Theme.of(context).colorScheme.primary,
              Theme.of(context).colorScheme.primary.withValues(alpha: 0.7),
            ],
          ),
        ),
        child: Column(
          children: [
            Icon(
              provider.isTimerRunning ? Icons.timer : Icons.timer_off,
              size: 48,
              color: Colors.white,
            ),
            const SizedBox(height: 8),
            Text(
              _formatElapsedTime(provider.elapsedTime),
              style: Theme.of(context).textTheme.displayMedium?.copyWith(
                color: Colors.white,
                fontWeight: FontWeight.bold,
                fontFeatures: [const FontFeature.tabularFigures()],
              ),
            ),
            const SizedBox(height: 4),
            Text(
              'Workout Duration',
              style: Theme.of(context).textTheme.bodyLarge?.copyWith(
                color: Colors.white.withValues(alpha: 0.9),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildEmptyState() {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(Icons.fitness_center, size: 80, color: Colors.grey.shade300),
          const SizedBox(height: 16),
          Text(
            'No Exercises Added',
            style: Theme.of(context).textTheme.titleLarge,
          ),
          const SizedBox(height: 8),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 48),
            child: Text(
              'Add exercises to your workout using the + button below',
              textAlign: TextAlign.center,
              style: Theme.of(
                context,
              ).textTheme.bodyMedium?.copyWith(color: Colors.grey),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildExercisesList(ActiveWorkoutProvider provider) {
    return ListView.builder(
      padding: const EdgeInsets.only(bottom: 80, left: 16, right: 16),
      itemCount: provider.exercises.length,
      itemBuilder: (context, index) {
        final exercise = provider.exercises[index];
        return Card(
          margin: const EdgeInsets.only(bottom: 12),
          child: ListTile(
            contentPadding: const EdgeInsets.symmetric(
              horizontal: 16,
              vertical: 8,
            ),
            leading: CircleAvatar(
              backgroundColor: Theme.of(
                context,
              ).colorScheme.primary.withValues(alpha: 0.1),
              child: Icon(
                Icons.fitness_center,
                color: Theme.of(context).colorScheme.primary,
              ),
            ),
            title: Text(
              exercise.name,
              style: const TextStyle(fontWeight: FontWeight.bold),
            ),
            subtitle: _buildExerciseSubtitle(exercise),
            trailing: const Icon(Icons.chevron_right),
            onTap: () => _handleExerciseTap(exercise.id),
          ),
        );
      },
    );
  }

  Widget? _buildExerciseSubtitle(dynamic exercise) {
    final List<String> details = [];

    if (exercise.sets != null && exercise.sets > 0) {
      details.add('${exercise.sets} sets');
    }
    if (exercise.reps != null && exercise.reps > 0) {
      details.add('${exercise.reps} reps');
    }
    if (exercise.weight != null && exercise.weight > 0) {
      details.add('${exercise.weight} lbs');
    }

    if (details.isEmpty) {
      return const Text('Tap to log sets');
    }

    return Text(details.join(' • '));
  }

  String _formatElapsedTime(Duration duration) {
    final hours = duration.inHours.toString().padLeft(2, '0');
    final minutes = duration.inMinutes.remainder(60).toString().padLeft(2, '0');
    final seconds = duration.inSeconds.remainder(60).toString().padLeft(2, '0');
    return '$hours:$minutes:$seconds';
  }
}
