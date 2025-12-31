import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../../providers/exercises_provider.dart';
import '../../../providers/active_workout_provider.dart';
import '../../../data/models/exercise.dart';
import '../../widgets/exercises/exercise_card.dart';

/// Add exercise screen for selecting exercises to add to active workout
/// Matches AddExercisePage.xaml from MAUI app
class AddExerciseScreen extends StatefulWidget {
  final int sessionId;

  const AddExerciseScreen({
    super.key,
    required this.sessionId,
  });

  @override
  State<AddExerciseScreen> createState() => _AddExerciseScreenState();
}

class _AddExerciseScreenState extends State<AddExerciseScreen> {
  final Set<int> _selectedExerciseIds = {};
  bool _isAdding = false;

  @override
  void initState() {
    super.initState();
    // Load exercises on first build
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<ExercisesProvider>().loadExercises();
    });
  }

  Future<void> _handleRefresh() async {
    await context.read<ExercisesProvider>().loadExercises();
  }

  void _toggleExerciseSelection(int exerciseId) {
    setState(() {
      if (_selectedExerciseIds.contains(exerciseId)) {
        _selectedExerciseIds.remove(exerciseId);
      } else {
        _selectedExerciseIds.add(exerciseId);
      }
    });
  }

  Future<void> _handleAddExercises() async {
    if (_selectedExerciseIds.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Please select at least one exercise'),
        ),
      );
      return;
    }

    setState(() {
      _isAdding = true;
    });

    try {
      final provider = context.read<ActiveWorkoutProvider>();
      final exercisesProvider = context.read<ExercisesProvider>();

      // Add each selected exercise to the workout
      for (final exerciseId in _selectedExerciseIds) {
        final template = exercisesProvider.exercises
            .firstWhere((e) => e.id == exerciseId);

        final exercise = Exercise(
          id: 0, // Will be assigned by server
          sessionId: widget.sessionId,
          name: template.name,
          exerciseTemplateId: template.id,
        );

        await provider.addExercise(exercise);
      }

      if (mounted) {
        // Return true to indicate exercises were added
        Navigator.of(context).pop(true);
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Error adding exercises: ${e.toString()}'),
            backgroundColor: Colors.red,
          ),
        );
      }
    } finally {
      if (mounted) {
        setState(() {
          _isAdding = false;
        });
      }
    }
  }

  void _showCategoryFilter() {
    final provider = context.read<ExercisesProvider>();

    showModalBottomSheet(
      context: context,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (context) => Container(
        padding: const EdgeInsets.all(16),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Filter by Category',
              style: Theme.of(context).textTheme.titleLarge?.copyWith(
                    fontWeight: FontWeight.bold,
                  ),
            ),
            const SizedBox(height: 16),
            Wrap(
              spacing: 8,
              runSpacing: 8,
              children: [
                _buildFilterChip(context, provider, null, 'All'),
                _buildFilterChip(context, provider, 'Strength', 'Strength'),
                _buildFilterChip(context, provider, 'Cardio', 'Cardio'),
                _buildFilterChip(context, provider, 'Flexibility', 'Flexibility'),
                _buildFilterChip(context, provider, 'Balance', 'Balance'),
                _buildFilterChip(context, provider, 'Core', 'Core'),
              ],
            ),
            const SizedBox(height: 16),
          ],
        ),
      ),
    );
  }

  Widget _buildFilterChip(
    BuildContext context,
    ExercisesProvider provider,
    String? category,
    String label,
  ) {
    final isSelected = provider.selectedCategory == category ||
        (category == null && provider.selectedCategory == null);

    return FilterChip(
      label: Text(label),
      selected: isSelected,
      onSelected: (selected) {
        provider.filterByCategory(category);
        Navigator.of(context).pop();
      },
      selectedColor: Theme.of(context).colorScheme.primary.withValues(alpha: 0.2),
      checkmarkColor: Theme.of(context).colorScheme.primary,
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Add Exercises'),
        actions: [
          IconButton(
            icon: const Icon(Icons.filter_list),
            onPressed: _showCategoryFilter,
            tooltip: 'Filter',
          ),
          if (_selectedExerciseIds.isNotEmpty)
            IconButton(
              icon: _isAdding
                  ? const SizedBox(
                      width: 20,
                      height: 20,
                      child: CircularProgressIndicator(
                        strokeWidth: 2,
                        color: Colors.white,
                      ),
                    )
                  : const Icon(Icons.check),
              onPressed: _isAdding ? null : _handleAddExercises,
              tooltip: 'Add Selected',
            ),
        ],
      ),
      body: Column(
        children: [
          // Selection counter
          if (_selectedExerciseIds.isNotEmpty)
            Container(
              width: double.infinity,
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
              color: Theme.of(context).colorScheme.primary.withValues(alpha: 0.1),
              child: Row(
                children: [
                  Icon(
                    Icons.check_circle,
                    color: Theme.of(context).colorScheme.primary,
                  ),
                  const SizedBox(width: 8),
                  Text(
                    '${_selectedExerciseIds.length} exercise${_selectedExerciseIds.length == 1 ? '' : 's'} selected',
                    style: TextStyle(
                      color: Theme.of(context).colorScheme.primary,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                  const Spacer(),
                  TextButton(
                    onPressed: () {
                      setState(() {
                        _selectedExerciseIds.clear();
                      });
                    },
                    child: const Text('Clear'),
                  ),
                ],
              ),
            ),

          // Exercise list
          Expanded(
            child: Consumer<ExercisesProvider>(
              builder: (context, provider, child) {
                // Loading state
                if (provider.isLoading && provider.exercises.isEmpty) {
                  return const Center(
                    child: CircularProgressIndicator(),
                  );
                }

                // Error state
                if (provider.errorMessage != null &&
                    provider.errorMessage!.isNotEmpty) {
                  return Center(
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Icon(
                          Icons.error_outline,
                          size: 64,
                          color: Colors.red.shade300,
                        ),
                        const SizedBox(height: 16),
                        Text(
                          'Error Loading Exercises',
                          style: Theme.of(context).textTheme.titleLarge,
                        ),
                        const SizedBox(height: 8),
                        Padding(
                          padding: const EdgeInsets.symmetric(horizontal: 32),
                          child: Text(
                            provider.errorMessage!,
                            textAlign: TextAlign.center,
                            style:
                                Theme.of(context).textTheme.bodyMedium?.copyWith(
                                      color: Colors.grey,
                                    ),
                          ),
                        ),
                        const SizedBox(height: 24),
                        ElevatedButton.icon(
                          onPressed: _handleRefresh,
                          icon: const Icon(Icons.refresh),
                          label: const Text('Retry'),
                        ),
                      ],
                    ),
                  );
                }

                // Empty state
                if (provider.filteredExercises.isEmpty) {
                  return Center(
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Icon(
                          Icons.search_off,
                          size: 80,
                          color: Colors.grey.shade300,
                        ),
                        const SizedBox(height: 16),
                        Text(
                          'No Exercises Found',
                          style: Theme.of(context).textTheme.titleLarge,
                        ),
                        const SizedBox(height: 8),
                        Text(
                          'Try selecting a different category',
                          textAlign: TextAlign.center,
                          style:
                              Theme.of(context).textTheme.bodyMedium?.copyWith(
                                    color: Colors.grey,
                                  ),
                        ),
                      ],
                    ),
                  );
                }

                // Exercise list
                return RefreshIndicator(
                  onRefresh: _handleRefresh,
                  child: ListView.builder(
                    physics: const AlwaysScrollableScrollPhysics(),
                    itemCount: provider.filteredExercises.length,
                    padding: const EdgeInsets.only(top: 8, bottom: 16),
                    itemBuilder: (context, index) {
                      final exercise = provider.filteredExercises[index];
                      final isSelected =
                          _selectedExerciseIds.contains(exercise.id);

                      return ExerciseCard(
                        exercise: exercise,
                        onTap: () => _toggleExerciseSelection(exercise.id),
                        trailing: Checkbox(
                          value: isSelected,
                          onChanged: (_) => _toggleExerciseSelection(exercise.id),
                        ),
                      );
                    },
                  ),
                );
              },
            ),
          ),
        ],
      ),
      floatingActionButton: _selectedExerciseIds.isNotEmpty
          ? FloatingActionButton.extended(
              onPressed: _isAdding ? null : _handleAddExercises,
              icon: _isAdding
                  ? const SizedBox(
                      width: 20,
                      height: 20,
                      child: CircularProgressIndicator(
                        strokeWidth: 2,
                        color: Colors.white,
                      ),
                    )
                  : const Icon(Icons.add),
              label: Text(
                'Add ${_selectedExerciseIds.length} Exercise${_selectedExerciseIds.length == 1 ? '' : 's'}',
              ),
            )
          : null,
    );
  }
}
