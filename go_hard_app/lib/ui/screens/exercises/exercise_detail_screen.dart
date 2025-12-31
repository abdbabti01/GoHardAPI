import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../../providers/exercise_detail_provider.dart';
import '../../../data/models/exercise_template.dart';
import '../../widgets/exercises/category_badge.dart';

/// Exercise detail screen for viewing exercise template information
/// Matches ExerciseDetailPage.xaml from MAUI app
class ExerciseDetailScreen extends StatefulWidget {
  final int exerciseId;

  const ExerciseDetailScreen({super.key, required this.exerciseId});

  @override
  State<ExerciseDetailScreen> createState() => _ExerciseDetailScreenState();
}

class _ExerciseDetailScreenState extends State<ExerciseDetailScreen> {
  @override
  void initState() {
    super.initState();
    _loadExercise();
  }

  Future<void> _loadExercise() async {
    await context.read<ExerciseDetailProvider>().loadExercise(
      widget.exerciseId,
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Consumer<ExerciseDetailProvider>(
        builder: (context, provider, child) {
          return CustomScrollView(
            slivers: [
              // App bar with image background
              SliverAppBar(
                expandedHeight: 200,
                pinned: true,
                flexibleSpace: FlexibleSpaceBar(
                  title:
                      provider.exercise != null
                          ? Text(
                            provider.exercise!.name,
                            style: const TextStyle(
                              fontWeight: FontWeight.bold,
                              shadows: [
                                Shadow(blurRadius: 8.0, color: Colors.black45),
                              ],
                            ),
                          )
                          : null,
                  background: _buildHeaderImage(provider.exercise),
                ),
              ),

              // Content
              if (provider.isLoading)
                const SliverFillRemaining(
                  child: Center(child: CircularProgressIndicator()),
                )
              else if (provider.errorMessage != null &&
                  provider.errorMessage!.isNotEmpty)
                SliverFillRemaining(
                  child: _buildErrorState(provider.errorMessage!),
                )
              else if (provider.exercise != null)
                _buildExerciseContent(provider.exercise!),
            ],
          );
        },
      ),
    );
  }

  Widget _buildHeaderImage(ExerciseTemplate? exercise) {
    if (exercise?.imageUrl != null && exercise!.imageUrl!.isNotEmpty) {
      return Stack(
        fit: StackFit.expand,
        children: [
          Image.network(
            exercise.imageUrl!,
            fit: BoxFit.cover,
            errorBuilder:
                (context, error, stackTrace) => _buildPlaceholderImage(),
          ),
          Container(
            decoration: BoxDecoration(
              gradient: LinearGradient(
                begin: Alignment.topCenter,
                end: Alignment.bottomCenter,
                colors: [
                  Colors.transparent,
                  Colors.black.withValues(alpha: 0.7),
                ],
              ),
            ),
          ),
        ],
      );
    }
    return _buildPlaceholderImage();
  }

  Widget _buildPlaceholderImage() {
    return Container(
      decoration: BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [
            Theme.of(context).colorScheme.primary,
            Theme.of(context).colorScheme.primary.withValues(alpha: 0.7),
          ],
        ),
      ),
      child: const Icon(Icons.fitness_center, size: 80, color: Colors.white),
    );
  }

  Widget _buildErrorState(String errorMessage) {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(Icons.error_outline, size: 64, color: Colors.red.shade300),
          const SizedBox(height: 16),
          Text(
            'Error Loading Exercise',
            style: Theme.of(context).textTheme.titleLarge,
          ),
          const SizedBox(height: 8),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 32),
            child: Text(
              errorMessage,
              textAlign: TextAlign.center,
              style: Theme.of(
                context,
              ).textTheme.bodyMedium?.copyWith(color: Colors.grey),
            ),
          ),
          const SizedBox(height: 24),
          ElevatedButton.icon(
            onPressed: _loadExercise,
            icon: const Icon(Icons.refresh),
            label: const Text('Retry'),
          ),
        ],
      ),
    );
  }

  Widget _buildExerciseContent(ExerciseTemplate exercise) {
    return SliverToBoxAdapter(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Badges
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: [
                if (exercise.category != null)
                  CategoryBadge(category: exercise.category),
                if (exercise.muscleGroup != null)
                  _buildInfoBadge(
                    exercise.muscleGroup!,
                    Colors.blue,
                    Icons.accessibility_new,
                  ),
                if (exercise.equipment != null)
                  _buildInfoBadge(
                    exercise.equipment!,
                    Colors.purple,
                    Icons.fitness_center,
                  ),
                if (exercise.difficulty != null)
                  _buildInfoBadge(
                    exercise.difficulty!,
                    _getDifficultyColor(exercise.difficulty!),
                    _getDifficultyIcon(exercise.difficulty!),
                  ),
              ],
            ),

            const SizedBox(height: 24),

            // Description
            if (exercise.description != null &&
                exercise.description!.isNotEmpty) ...[
              Text(
                'Description',
                style: Theme.of(
                  context,
                ).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.bold),
              ),
              const SizedBox(height: 8),
              Text(
                exercise.description!,
                style: Theme.of(context).textTheme.bodyLarge,
              ),
              const SizedBox(height: 24),
            ],

            // Instructions
            if (exercise.instructions != null &&
                exercise.instructions!.isNotEmpty) ...[
              Text(
                'Instructions',
                style: Theme.of(
                  context,
                ).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.bold),
              ),
              const SizedBox(height: 12),
              ..._buildInstructionsList(exercise.instructions!),
              const SizedBox(height: 24),
            ],

            // Video link
            if (exercise.videoUrl != null && exercise.videoUrl!.isNotEmpty) ...[
              Card(
                child: ListTile(
                  leading: Icon(
                    Icons.play_circle_outline,
                    color: Theme.of(context).colorScheme.primary,
                    size: 32,
                  ),
                  title: const Text('Watch Video Tutorial'),
                  subtitle: const Text('Learn proper form and technique'),
                  trailing: const Icon(Icons.open_in_new),
                  onTap: () {
                    // Open video URL
                    // In a real app, you would use url_launcher package
                    ScaffoldMessenger.of(context).showSnackBar(
                      SnackBar(
                        content: Text('Video URL: ${exercise.videoUrl}'),
                      ),
                    );
                  },
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }

  Widget _buildInfoBadge(String label, Color color, IconData icon) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.15),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: color.withValues(alpha: 0.3), width: 1),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 14, color: color),
          const SizedBox(width: 4),
          Text(
            label,
            style: TextStyle(
              color: color,
              fontSize: 12,
              fontWeight: FontWeight.w600,
            ),
          ),
        ],
      ),
    );
  }

  List<Widget> _buildInstructionsList(String instructions) {
    final steps =
        instructions.split('\n').where((s) => s.trim().isNotEmpty).toList();

    return List.generate(steps.length, (index) {
      return Padding(
        padding: const EdgeInsets.only(bottom: 12),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Container(
              width: 28,
              height: 28,
              decoration: BoxDecoration(
                color: Theme.of(context).colorScheme.primary,
                shape: BoxShape.circle,
              ),
              child: Center(
                child: Text(
                  '${index + 1}',
                  style: const TextStyle(
                    color: Colors.white,
                    fontWeight: FontWeight.bold,
                    fontSize: 14,
                  ),
                ),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Padding(
                padding: const EdgeInsets.only(top: 4),
                child: Text(
                  steps[index].trim(),
                  style: Theme.of(context).textTheme.bodyMedium,
                ),
              ),
            ),
          ],
        ),
      );
    });
  }

  IconData _getDifficultyIcon(String difficulty) {
    switch (difficulty.toLowerCase()) {
      case 'beginner':
        return Icons.signal_cellular_alt_1_bar;
      case 'intermediate':
        return Icons.signal_cellular_alt_2_bar;
      case 'advanced':
        return Icons.signal_cellular_alt;
      default:
        return Icons.signal_cellular_alt_2_bar;
    }
  }

  Color _getDifficultyColor(String difficulty) {
    switch (difficulty.toLowerCase()) {
      case 'beginner':
        return Colors.green;
      case 'intermediate':
        return Colors.orange;
      case 'advanced':
        return Colors.red;
      default:
        return Colors.grey;
    }
  }
}
