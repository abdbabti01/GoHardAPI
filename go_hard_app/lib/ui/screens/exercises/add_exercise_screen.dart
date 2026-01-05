import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';
import '../../../providers/exercises_provider.dart';
import '../../../providers/active_workout_provider.dart';
import '../../widgets/exercises/exercise_card.dart';

/// Add exercise screen for selecting exercises to add to active workout
/// Matches AddExercisePage.xaml from MAUI app
class AddExerciseScreen extends StatefulWidget {
  final int sessionId;

  const AddExerciseScreen({super.key, required this.sessionId});

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
    // Haptic feedback for better UX
    HapticFeedback.selectionClick();

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
        const SnackBar(content: Text('Please select at least one exercise')),
      );
      return;
    }

    // Haptic feedback when adding
    HapticFeedback.mediumImpact();

    setState(() {
      _isAdding = true;
    });

    // Capture context-dependent objects before async operations
    final scaffoldMessenger = ScaffoldMessenger.of(context);
    final navigator = Navigator.of(context);

    try {
      final provider = context.read<ActiveWorkoutProvider>();

      // Add each selected exercise to the workout
      for (final exerciseId in _selectedExerciseIds) {
        // Send just the template ID - API will create the exercise
        await provider.addExercise(exerciseId);
      }

      if (mounted) {
        // Success haptic feedback
        HapticFeedback.heavyImpact();

        // Show success message
        scaffoldMessenger.showSnackBar(
          SnackBar(
            content: Row(
              children: [
                const Icon(Icons.check_circle, color: Colors.white),
                const SizedBox(width: 12),
                Text(
                  '${_selectedExerciseIds.length} exercise${_selectedExerciseIds.length == 1 ? '' : 's'} added!',
                ),
              ],
            ),
            backgroundColor: Colors.green,
            behavior: SnackBarBehavior.floating,
            duration: const Duration(seconds: 2),
          ),
        );

        // Small delay for user to see the success message
        await Future.delayed(const Duration(milliseconds: 300));

        // Return true to indicate exercises were added
        if (mounted) {
          navigator.pop(true);
        }
      }
    } catch (e) {
      if (mounted) {
        scaffoldMessenger.showSnackBar(
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
      builder:
          (context) => Container(
            padding: const EdgeInsets.all(16),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Filter by Category',
                  style: Theme.of(
                    context,
                  ).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.bold),
                ),
                const SizedBox(height: 16),
                Wrap(
                  spacing: 8,
                  runSpacing: 8,
                  children: [
                    _buildFilterChip(context, provider, null, 'All'),
                    _buildFilterChip(context, provider, 'Strength', 'Strength'),
                    _buildFilterChip(context, provider, 'Cardio', 'Cardio'),
                    _buildFilterChip(
                      context,
                      provider,
                      'Flexibility',
                      'Flexibility',
                    ),
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
    final isSelected =
        provider.selectedCategory == category ||
        (category == null && provider.selectedCategory == null);

    return FilterChip(
      label: Text(label),
      selected: isSelected,
      onSelected: (selected) {
        provider.filterByCategory(category);
        Navigator.of(context).pop();
      },
      selectedColor: Theme.of(
        context,
      ).colorScheme.primary.withValues(alpha: 0.2),
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
              icon:
                  _isAdding
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
          // Selection counter with smooth animation
          AnimatedSize(
            duration: const Duration(milliseconds: 200),
            curve: Curves.easeInOut,
            child: AnimatedSwitcher(
              duration: const Duration(milliseconds: 200),
              transitionBuilder: (child, animation) {
                return SizeTransition(
                  sizeFactor: animation,
                  child: FadeTransition(opacity: animation, child: child),
                );
              },
              child:
                  _selectedExerciseIds.isNotEmpty
                      ? Container(
                        key: const ValueKey('selection-banner'),
                        width: double.infinity,
                        padding: const EdgeInsets.symmetric(
                          horizontal: 16,
                          vertical: 12,
                        ),
                        decoration: BoxDecoration(
                          color: Theme.of(
                            context,
                          ).colorScheme.primary.withValues(alpha: 0.1),
                          border: Border(
                            bottom: BorderSide(
                              color: Theme.of(
                                context,
                              ).colorScheme.primary.withValues(alpha: 0.2),
                              width: 1,
                            ),
                          ),
                        ),
                        child: Row(
                          children: [
                            Icon(
                              Icons.check_circle,
                              color: Theme.of(context).colorScheme.primary,
                              size: 22,
                            ),
                            const SizedBox(width: 12),
                            Expanded(
                              child: AnimatedSwitcher(
                                duration: const Duration(milliseconds: 200),
                                child: Text(
                                  '${_selectedExerciseIds.length} exercise${_selectedExerciseIds.length == 1 ? '' : 's'} selected',
                                  key: ValueKey(_selectedExerciseIds.length),
                                  style: TextStyle(
                                    color:
                                        Theme.of(context).colorScheme.primary,
                                    fontWeight: FontWeight.w600,
                                    fontSize: 15,
                                  ),
                                ),
                              ),
                            ),
                            TextButton.icon(
                              onPressed: () {
                                HapticFeedback.lightImpact();
                                setState(() {
                                  _selectedExerciseIds.clear();
                                });
                              },
                              icon: const Icon(Icons.clear, size: 18),
                              label: const Text('Clear'),
                              style: TextButton.styleFrom(
                                foregroundColor:
                                    Theme.of(context).colorScheme.primary,
                              ),
                            ),
                          ],
                        ),
                      )
                      : const SizedBox(key: ValueKey('empty-banner')),
            ),
          ),

          // Exercise list
          Expanded(
            child: Consumer<ExercisesProvider>(
              builder: (context, provider, child) {
                // Loading state
                if (provider.isLoading && provider.exercises.isEmpty) {
                  return const Center(child: CircularProgressIndicator());
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
                            style: Theme.of(context).textTheme.bodyMedium
                                ?.copyWith(color: Colors.grey),
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
                          style: Theme.of(
                            context,
                          ).textTheme.bodyMedium?.copyWith(color: Colors.grey),
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
                      final isSelected = _selectedExerciseIds.contains(
                        exercise.id,
                      );

                      return ExerciseCard(
                        exercise: exercise,
                        onTap: () => _toggleExerciseSelection(exercise.id),
                        trailing: Checkbox(
                          value: isSelected,
                          onChanged:
                              (_) => _toggleExerciseSelection(exercise.id),
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
      floatingActionButton: AnimatedScale(
        scale: _selectedExerciseIds.isNotEmpty ? 1.0 : 0.0,
        duration: const Duration(milliseconds: 250),
        curve: Curves.elasticOut,
        child: AnimatedOpacity(
          opacity: _selectedExerciseIds.isNotEmpty ? 1.0 : 0.0,
          duration: const Duration(milliseconds: 200),
          child:
              _selectedExerciseIds.isNotEmpty
                  ? Container(
                    decoration: BoxDecoration(
                      borderRadius: BorderRadius.circular(30),
                      gradient: LinearGradient(
                        colors:
                            _isAdding
                                ? [Colors.grey.shade400, Colors.grey.shade500]
                                : [
                                  Theme.of(context).colorScheme.primary,
                                  Theme.of(
                                    context,
                                  ).colorScheme.primary.withValues(alpha: 0.8),
                                ],
                        begin: Alignment.topLeft,
                        end: Alignment.bottomRight,
                      ),
                      boxShadow: [
                        BoxShadow(
                          color: Theme.of(
                            context,
                          ).colorScheme.primary.withValues(alpha: 0.4),
                          blurRadius: 12,
                          offset: const Offset(0, 4),
                        ),
                      ],
                    ),
                    child: Material(
                      color: Colors.transparent,
                      child: InkWell(
                        borderRadius: BorderRadius.circular(30),
                        onTap: _isAdding ? null : _handleAddExercises,
                        child: Padding(
                          padding: const EdgeInsets.symmetric(
                            horizontal: 24,
                            vertical: 16,
                          ),
                          child: Row(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              AnimatedSwitcher(
                                duration: const Duration(milliseconds: 200),
                                child:
                                    _isAdding
                                        ? const SizedBox(
                                          width: 24,
                                          height: 24,
                                          child: CircularProgressIndicator(
                                            strokeWidth: 3,
                                            color: Colors.white,
                                          ),
                                        )
                                        : Container(
                                          padding: const EdgeInsets.all(6),
                                          decoration: BoxDecoration(
                                            color: Colors.white.withValues(
                                              alpha: 0.2,
                                            ),
                                            shape: BoxShape.circle,
                                          ),
                                          child: const Icon(
                                            Icons.add_rounded,
                                            color: Colors.white,
                                            size: 20,
                                          ),
                                        ),
                              ),
                              const SizedBox(width: 12),
                              AnimatedSwitcher(
                                duration: const Duration(milliseconds: 200),
                                transitionBuilder: (child, animation) {
                                  return FadeTransition(
                                    opacity: animation,
                                    child: ScaleTransition(
                                      scale: animation,
                                      child: child,
                                    ),
                                  );
                                },
                                child: Text(
                                  _isAdding
                                      ? 'Adding...'
                                      : 'Add ${_selectedExerciseIds.length} Exercise${_selectedExerciseIds.length == 1 ? '' : 's'}',
                                  key: ValueKey(_isAdding),
                                  style: const TextStyle(
                                    fontSize: 17,
                                    fontWeight: FontWeight.bold,
                                    color: Colors.white,
                                    letterSpacing: 0.5,
                                  ),
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),
                    ),
                  )
                  : const SizedBox.shrink(),
        ),
      ),
    );
  }
}
