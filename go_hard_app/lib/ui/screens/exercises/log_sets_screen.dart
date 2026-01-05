import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../../providers/log_sets_provider.dart';
import '../../../data/models/exercise_set.dart';

/// Log sets screen for adding and managing exercise sets
/// Matches LogSetsPage.xaml from MAUI app
class LogSetsScreen extends StatefulWidget {
  final int exerciseId;

  const LogSetsScreen({super.key, required this.exerciseId});

  @override
  State<LogSetsScreen> createState() => _LogSetsScreenState();
}

class _LogSetsScreenState extends State<LogSetsScreen> {
  final _repsController = TextEditingController();
  final _weightController = TextEditingController();

  @override
  void initState() {
    super.initState();
    // Load sets on first build
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<LogSetsProvider>().loadSets(widget.exerciseId);
    });
  }

  @override
  void dispose() {
    _repsController.dispose();
    _weightController.dispose();
    super.dispose();
  }

  Future<void> _handleAddSet() async {
    final reps = int.tryParse(_repsController.text);
    final weight = double.tryParse(_weightController.text);

    if (reps == null || reps <= 0) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Please enter valid reps'),
          backgroundColor: Colors.red,
        ),
      );
      return;
    }

    if (weight == null || weight < 0) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Please enter valid weight'),
          backgroundColor: Colors.red,
        ),
      );
      return;
    }

    // Capture context-dependent objects before async operation
    final scaffoldMessenger = ScaffoldMessenger.of(context);
    final focusScope = FocusScope.of(context);

    final success = await context.read<LogSetsProvider>().addSet(
      exerciseId: widget.exerciseId,
      reps: reps,
      weight: weight,
    );

    if (success && mounted) {
      // Clear inputs after successful add
      _repsController.clear();
      _weightController.clear();

      // Hide keyboard
      focusScope.unfocus();

      scaffoldMessenger.showSnackBar(
        const SnackBar(
          content: Text('Set added successfully'),
          backgroundColor: Colors.green,
          duration: Duration(seconds: 1),
        ),
      );
    }
  }

  Future<void> _handleCompleteSet(ExerciseSet set) async {
    await context.read<LogSetsProvider>().completeSet(set);
  }

  Future<void> _handleDeleteSet(ExerciseSet set) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder:
          (context) => AlertDialog(
            title: const Text('Delete Set'),
            content: Text('Delete Set ${set.setNumber}?'),
            actions: [
              TextButton(
                onPressed: () => Navigator.of(context).pop(false),
                child: const Text('Cancel'),
              ),
              TextButton(
                onPressed: () => Navigator.of(context).pop(true),
                style: TextButton.styleFrom(foregroundColor: Colors.red),
                child: const Text('Delete'),
              ),
            ],
          ),
    );

    if (confirmed == true && mounted) {
      final provider = context.read<LogSetsProvider>();
      final scaffoldMessenger = ScaffoldMessenger.of(context);

      final success = await provider.deleteSet(set);

      if (success && mounted) {
        scaffoldMessenger.showSnackBar(
          const SnackBar(
            content: Text('Set deleted'),
            duration: Duration(seconds: 1),
          ),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Log Sets')),
      body: Consumer<LogSetsProvider>(
        builder: (context, provider, child) {
          return Column(
            children: [
              // Add set form
              Card(
                margin: const EdgeInsets.all(16),
                elevation: 4,
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Add New Set',
                        style: Theme.of(context).textTheme.titleMedium
                            ?.copyWith(fontWeight: FontWeight.bold),
                      ),
                      const SizedBox(height: 16),
                      Row(
                        children: [
                          // Reps input
                          Expanded(
                            child: TextField(
                              controller: _repsController,
                              keyboardType: TextInputType.number,
                              decoration: InputDecoration(
                                labelText: 'Reps',
                                hintText: '10',
                                prefixIcon: const Icon(Icons.repeat),
                                border: OutlineInputBorder(
                                  borderRadius: BorderRadius.circular(12),
                                ),
                              ),
                            ),
                          ),
                          const SizedBox(width: 12),

                          // Weight input
                          Expanded(
                            child: TextField(
                              controller: _weightController,
                              keyboardType: TextInputType.numberWithOptions(
                                decimal: true,
                              ),
                              decoration: InputDecoration(
                                labelText: 'Weight (lbs)',
                                hintText: '100',
                                prefixIcon: const Icon(Icons.scale),
                                border: OutlineInputBorder(
                                  borderRadius: BorderRadius.circular(12),
                                ),
                              ),
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 16),

                      // Add button
                      SizedBox(
                        width: double.infinity,
                        child: ElevatedButton.icon(
                          onPressed: provider.isLoading ? null : _handleAddSet,
                          icon: const Icon(Icons.add),
                          label: const Text('Add Set'),
                          style: ElevatedButton.styleFrom(
                            padding: const EdgeInsets.symmetric(vertical: 12),
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(12),
                            ),
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ),

              // Error message
              if (provider.errorMessage != null &&
                  provider.errorMessage!.isNotEmpty)
                Container(
                  margin: const EdgeInsets.symmetric(horizontal: 16),
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(
                    color: Colors.red.shade50,
                    borderRadius: BorderRadius.circular(8),
                    border: Border.all(color: Colors.red.shade200),
                  ),
                  child: Row(
                    children: [
                      Icon(Icons.error_outline, color: Colors.red.shade700),
                      const SizedBox(width: 12),
                      Expanded(
                        child: Text(
                          provider.errorMessage!,
                          style: TextStyle(color: Colors.red.shade700),
                        ),
                      ),
                    ],
                  ),
                ),

              const SizedBox(height: 8),

              // Sets list header
              if (provider.sets.isNotEmpty)
                Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 16),
                  child: Row(
                    children: [
                      Text(
                        'Sets (${provider.sets.length})',
                        style: Theme.of(context).textTheme.titleMedium
                            ?.copyWith(fontWeight: FontWeight.bold),
                      ),
                      const Spacer(),
                      Text(
                        '${provider.sets.where((s) => s.isCompleted).length} completed',
                        style: Theme.of(context).textTheme.bodySmall?.copyWith(
                          color: Colors.grey.shade600,
                        ),
                      ),
                    ],
                  ),
                ),

              const SizedBox(height: 8),

              // Sets list
              Expanded(
                child:
                    provider.isLoading && provider.sets.isEmpty
                        ? const Center(child: CircularProgressIndicator())
                        : provider.sets.isEmpty
                        ? _buildEmptyState()
                        : _buildSetsList(provider),
              ),
            ],
          );
        },
      ),
    );
  }

  Widget _buildEmptyState() {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(Icons.playlist_add, size: 80, color: Colors.grey.shade300),
          const SizedBox(height: 16),
          Text('No Sets Logged', style: Theme.of(context).textTheme.titleLarge),
          const SizedBox(height: 8),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 48),
            child: Text(
              'Add your first set using the form above',
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

  Widget _buildSetsList(LogSetsProvider provider) {
    return ListView.builder(
      padding: const EdgeInsets.symmetric(horizontal: 16),
      itemCount: provider.sets.length,
      itemBuilder: (context, index) {
        final set = provider.sets[index];
        return Card(
          margin: const EdgeInsets.only(bottom: 8),
          child: ListTile(
            leading: CircleAvatar(
              backgroundColor:
                  set.isCompleted
                      ? Colors.green.withValues(alpha: 0.2)
                      : Theme.of(
                        context,
                      ).colorScheme.primary.withValues(alpha: 0.2),
              child:
                  set.isCompleted
                      ? const Icon(Icons.check, color: Colors.green)
                      : Text(
                        '${set.setNumber}',
                        style: TextStyle(
                          color: Theme.of(context).colorScheme.primary,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
            ),
            title: Text(
              '${set.reps} reps × ${set.weight} lbs',
              style: TextStyle(
                fontWeight: FontWeight.bold,
                decoration:
                    set.isCompleted
                        ? TextDecoration.lineThrough
                        : TextDecoration.none,
              ),
            ),
            subtitle:
                set.completedAt != null
                    ? Text(
                      'Completed at ${_formatTime(set.completedAt!)}',
                      style: const TextStyle(fontSize: 12),
                    )
                    : null,
            trailing: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                if (!set.isCompleted)
                  IconButton(
                    icon: const Icon(Icons.check_circle_outline),
                    color: Colors.green,
                    onPressed: () => _handleCompleteSet(set),
                    tooltip: 'Mark Complete',
                  ),
                IconButton(
                  icon: const Icon(Icons.delete_outline),
                  color: Colors.red,
                  onPressed: () => _handleDeleteSet(set),
                  tooltip: 'Delete',
                ),
              ],
            ),
          ),
        );
      },
    );
  }

  String _formatTime(DateTime time) {
    final hour = time.hour.toString().padLeft(2, '0');
    final minute = time.minute.toString().padLeft(2, '0');
    return '$hour:$minute';
  }
}
