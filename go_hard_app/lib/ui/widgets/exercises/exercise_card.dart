import 'package:flutter/material.dart';
import '../../../data/models/exercise_template.dart';
import 'category_badge.dart';

/// Card widget for displaying an exercise template
/// Matches ExerciseCard from MAUI app
class ExerciseCard extends StatelessWidget {
  final ExerciseTemplate exercise;
  final VoidCallback? onTap;
  final Widget? trailing;

  const ExerciseCard({
    super.key,
    required this.exercise,
    this.onTap,
    this.trailing,
  });

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      elevation: 2,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Row(
            children: [
              // Exercise icon
              Container(
                width: 56,
                height: 56,
                decoration: BoxDecoration(
                  color: Theme.of(
                    context,
                  ).colorScheme.primary.withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Icon(
                  _getExerciseIcon(),
                  size: 28,
                  color: Theme.of(context).colorScheme.primary,
                ),
              ),
              const SizedBox(width: 16),

              // Exercise details
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    // Exercise name
                    Text(
                      exercise.name,
                      style: Theme.of(context).textTheme.titleMedium?.copyWith(
                        fontWeight: FontWeight.bold,
                      ),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                    ),
                    const SizedBox(height: 4),

                    // Category and muscle group
                    Wrap(
                      spacing: 8,
                      runSpacing: 4,
                      children: [
                        if (exercise.category != null)
                          CategoryBadge(category: exercise.category),
                        if (exercise.muscleGroup != null)
                          _buildInfoChip(
                            context,
                            exercise.muscleGroup!,
                            Colors.blue,
                          ),
                      ],
                    ),

                    // Equipment and difficulty
                    if (exercise.equipment != null ||
                        exercise.difficulty != null) ...[
                      const SizedBox(height: 8),
                      Row(
                        children: [
                          if (exercise.equipment != null) ...[
                            Icon(
                              Icons.fitness_center,
                              size: 14,
                              color: Colors.grey.shade600,
                            ),
                            const SizedBox(width: 4),
                            Text(
                              exercise.equipment!,
                              style: Theme.of(context).textTheme.bodySmall
                                  ?.copyWith(color: Colors.grey.shade600),
                            ),
                          ],
                          if (exercise.equipment != null &&
                              exercise.difficulty != null)
                            Padding(
                              padding: const EdgeInsets.symmetric(
                                horizontal: 8,
                              ),
                              child: Text(
                                '•',
                                style: TextStyle(color: Colors.grey.shade400),
                              ),
                            ),
                          if (exercise.difficulty != null) ...[
                            Icon(
                              _getDifficultyIcon(exercise.difficulty!),
                              size: 14,
                              color: _getDifficultyColor(exercise.difficulty!),
                            ),
                            const SizedBox(width: 4),
                            Text(
                              exercise.difficulty!,
                              style: Theme.of(
                                context,
                              ).textTheme.bodySmall?.copyWith(
                                color: _getDifficultyColor(
                                  exercise.difficulty!,
                                ),
                              ),
                            ),
                          ],
                        ],
                      ),
                    ],
                  ],
                ),
              ),

              // Trailing widget (optional)
              if (trailing != null) trailing!,
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildInfoChip(BuildContext context, String label, Color color) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.1),
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: color.withValues(alpha: 0.3), width: 1),
      ),
      child: Text(
        label,
        style: TextStyle(
          color: color,
          fontSize: 11,
          fontWeight: FontWeight.w600,
        ),
      ),
    );
  }

  IconData _getExerciseIcon() {
    if (exercise.category == null) return Icons.fitness_center;

    switch (exercise.category!.toLowerCase()) {
      case 'strength':
        return Icons.fitness_center;
      case 'cardio':
        return Icons.directions_run;
      case 'flexibility':
        return Icons.self_improvement;
      case 'balance':
        return Icons.accessibility_new;
      default:
        return Icons.fitness_center;
    }
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
