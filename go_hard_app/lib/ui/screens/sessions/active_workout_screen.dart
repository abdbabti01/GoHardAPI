import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../../providers/active_workout_provider.dart';
import '../../../providers/music_player_provider.dart';
import '../../../routes/route_names.dart';
import '../../widgets/common/offline_banner.dart';
import '../../widgets/music/music_control_widget.dart';

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
    // Load session (timer will auto-start if in_progress)
    WidgetsBinding.instance.addPostFrameCallback((_) {
      final provider = context.read<ActiveWorkoutProvider>();
      provider.loadSession(widget.sessionId);

      // Initialize music player for workout
      final musicProvider = context.read<MusicPlayerProvider>();
      if (!musicProvider.isInitialized) {
        musicProvider.initialize();
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

  Future<void> _handleEditWorkoutName() async {
    final provider = context.read<ActiveWorkoutProvider>();
    final currentName = provider.currentSession?.name;

    final newName = await showDialog<String>(
      context: context,
      builder: (dialogContext) {
        // Create controller inside builder so it's owned by dialog's lifecycle
        final controller = TextEditingController(text: currentName);

        return AlertDialog(
          title: const Text('Edit Workout Name'),
          content: TextField(
            autofocus: true,
            decoration: const InputDecoration(
              labelText: 'Workout Name',
              hintText: 'e.g., Push Day, Leg Day',
            ),
            controller: controller,
            textCapitalization: TextCapitalization.words,
            onSubmitted: (value) {
              controller.dispose();
              Navigator.of(dialogContext).pop(value);
            },
          ),
          actions: [
            TextButton(
              onPressed: () {
                controller.dispose();
                Navigator.of(dialogContext).pop();
              },
              child: const Text('Cancel'),
            ),
            TextButton(
              onPressed: () {
                final text = controller.text;
                controller.dispose();
                Navigator.of(dialogContext).pop(text);
              },
              child: const Text('Save'),
            ),
          ],
        );
      },
    );

    if (newName != null && newName.trim().isNotEmpty && mounted) {
      await provider.updateWorkoutName(newName.trim());
    }
  }

  void _handleExerciseTap(int exerciseId) {
    Navigator.of(context).pushNamed(RouteNames.logSets, arguments: exerciseId);
  }

  @override
  Widget build(BuildContext context) {
    return Consumer<ActiveWorkoutProvider>(
      builder: (context, provider, child) {
        return Scaffold(
          appBar: AppBar(
            title: Row(
              children: [
                Expanded(
                  child: Text(
                    provider.currentSession?.name ?? 'Active Workout',
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
                IconButton(
                  icon: const Icon(Icons.edit, size: 20),
                  onPressed: _handleEditWorkoutName,
                  tooltip: 'Edit Workout Name',
                ),
              ],
            ),
            actions: [
              IconButton(
                icon: const Icon(Icons.check_circle),
                onPressed: _handleFinishWorkout,
                tooltip: 'Finish Workout',
              ),
            ],
          ),
          body: Column(
            children: [
              const OfflineBanner(),
              Expanded(child: _buildBody(provider)),
            ],
          ),
          floatingActionButton: Container(
            height: 56,
            decoration: BoxDecoration(
              borderRadius: BorderRadius.circular(28),
              gradient: LinearGradient(
                colors: [
                  Theme.of(context).colorScheme.secondary,
                  Theme.of(
                    context,
                  ).colorScheme.secondary.withValues(alpha: 0.8),
                ],
              ),
              boxShadow: [
                BoxShadow(
                  color: Theme.of(
                    context,
                  ).colorScheme.secondary.withValues(alpha: 0.4),
                  blurRadius: 12,
                  offset: const Offset(0, 4),
                ),
              ],
            ),
            child: Material(
              color: Colors.transparent,
              child: InkWell(
                borderRadius: BorderRadius.circular(28),
                onTap: _handleAddExercise,
                child: Padding(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 24,
                    vertical: 16,
                  ),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(
                        Icons.add_circle_outline,
                        color: Theme.of(context).colorScheme.onSecondary,
                        size: 24,
                      ),
                      const SizedBox(width: 12),
                      Text(
                        'Add Exercise',
                        style: TextStyle(
                          color: Theme.of(context).colorScheme.onSecondary,
                          fontSize: 16,
                          fontWeight: FontWeight.w600,
                          letterSpacing: 0.5,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
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
        // Music player controls
        const MusicControlWidget(),

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
    final isDraft = provider.currentSession?.status == 'draft';

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
              color: Theme.of(context).colorScheme.onPrimary,
            ),
            const SizedBox(height: 8),
            Text(
              _formatElapsedTime(provider.elapsedTime),
              style: Theme.of(context).textTheme.displayMedium?.copyWith(
                color: Theme.of(context).colorScheme.onPrimary,
                fontWeight: FontWeight.bold,
                fontFeatures: [const FontFeature.tabularFigures()],
              ),
            ),
            const SizedBox(height: 4),
            Text(
              'Workout Duration',
              style: Theme.of(context).textTheme.bodyLarge?.copyWith(
                color: Theme.of(
                  context,
                ).colorScheme.onPrimary.withValues(alpha: 0.9),
              ),
            ),
            const SizedBox(height: 16),
            // Start/Pause button
            if (isDraft)
              ElevatedButton.icon(
                onPressed: provider.startWorkout,
                icon: const Icon(Icons.play_arrow),
                label: const Text('Start Workout'),
                style: ElevatedButton.styleFrom(
                  backgroundColor: Theme.of(context).colorScheme.onPrimary,
                  foregroundColor: Theme.of(context).colorScheme.primary,
                  padding: const EdgeInsets.symmetric(
                    horizontal: 32,
                    vertical: 12,
                  ),
                ),
              )
            else
              ElevatedButton.icon(
                onPressed:
                    provider.isTimerRunning
                        ? provider.pauseTimer
                        : provider.resumeTimer,
                icon: Icon(
                  provider.isTimerRunning ? Icons.pause : Icons.play_arrow,
                ),
                label: Text(provider.isTimerRunning ? 'Pause' : 'Resume'),
                style: ElevatedButton.styleFrom(
                  backgroundColor: Theme.of(context).colorScheme.onPrimary,
                  foregroundColor: Theme.of(context).colorScheme.primary,
                  padding: const EdgeInsets.symmetric(
                    horizontal: 32,
                    vertical: 12,
                  ),
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
    // Show count of logged sets
    final setsCount = exercise.exerciseSets?.length ?? 0;

    if (setsCount == 0) {
      return const Text('Tap to log sets');
    }

    return Text('$setsCount set${setsCount == 1 ? '' : 's'} logged');
  }

  String _formatElapsedTime(Duration duration) {
    final hours = duration.inHours.toString().padLeft(2, '0');
    final minutes = duration.inMinutes.remainder(60).toString().padLeft(2, '0');
    final seconds = duration.inSeconds.remainder(60).toString().padLeft(2, '0');
    return '$hours:$minutes:$seconds';
  }
}
