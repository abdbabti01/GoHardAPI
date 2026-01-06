import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import '../../../providers/goals_provider.dart';
import '../../../data/models/goal.dart';
import '../../../data/models/goal_progress.dart';

class GoalsScreen extends StatefulWidget {
  const GoalsScreen({super.key});

  @override
  State<GoalsScreen> createState() => _GoalsScreenState();
}

class _GoalsScreenState extends State<GoalsScreen>
    with SingleTickerProviderStateMixin {
  late TabController _tabController;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 2, vsync: this);

    // Load goals data
    WidgetsBinding.instance.addPostFrameCallback((_) {
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
    final provider = context.watch<GoalsProvider>();

    return Scaffold(
      appBar: AppBar(
        title: const Text('Goals'),
        bottom: TabBar(
          controller: _tabController,
          tabs: const [
            Tab(text: 'Active', icon: Icon(Icons.flag)),
            Tab(text: 'Completed', icon: Icon(Icons.check_circle)),
          ],
        ),
        actions: [
          IconButton(
            icon: const Icon(Icons.add),
            onPressed: () => _showCreateGoalDialog(context),
            tooltip: 'Create Goal',
          ),
        ],
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
                      onPressed: () => provider.loadGoals(),
                      icon: const Icon(Icons.refresh),
                      label: const Text('Retry'),
                    ),
                  ],
                ),
              )
              : TabBarView(
                controller: _tabController,
                children: [
                  _buildGoalsList(provider.activeGoals, true),
                  _buildGoalsList(provider.completedGoals, false),
                ],
              ),
    );
  }

  Widget _buildGoalsList(List<Goal> goals, bool isActive) {
    if (goals.isEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(
              isActive ? Icons.flag_outlined : Icons.check_circle_outline,
              size: 64,
              color: Colors.grey,
            ),
            const SizedBox(height: 16),
            Text(
              isActive ? 'No active goals' : 'No completed goals',
              style: const TextStyle(fontSize: 18, color: Colors.grey),
            ),
            if (isActive) ...[
              const SizedBox(height: 8),
              const Text(
                'Tap + to create your first goal',
                style: TextStyle(color: Colors.grey),
              ),
            ],
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: () => context.read<GoalsProvider>().loadGoals(),
      child: ListView.builder(
        padding: const EdgeInsets.all(16),
        itemCount: goals.length,
        itemBuilder: (context, index) => _buildGoalCard(goals[index]),
      ),
    );
  }

  Widget _buildGoalCard(Goal goal) {
    final progress = goal.progressPercentage.clamp(0.0, 100.0);
    final dateFormat = DateFormat('MMM d, y');

    return Card(
      margin: const EdgeInsets.only(bottom: 16),
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
                    _formatGoalType(goal.goalType),
                    style: const TextStyle(
                      fontSize: 18,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
                if (goal.isCompleted)
                  const Icon(Icons.check_circle, color: Colors.green)
                else
                  PopupMenuButton<String>(
                    itemBuilder:
                        (context) => [
                          const PopupMenuItem(
                            value: 'progress',
                            child: Row(
                              children: [
                                Icon(Icons.add_circle_outline),
                                SizedBox(width: 8),
                                Text('Add Progress'),
                              ],
                            ),
                          ),
                          const PopupMenuItem(
                            value: 'complete',
                            child: Row(
                              children: [
                                Icon(Icons.check_circle_outline),
                                SizedBox(width: 8),
                                Text('Mark Complete'),
                              ],
                            ),
                          ),
                          const PopupMenuItem(
                            value: 'delete',
                            child: Row(
                              children: [
                                Icon(Icons.delete_outline, color: Colors.red),
                                SizedBox(width: 8),
                                Text('Delete'),
                              ],
                            ),
                          ),
                        ],
                    onSelected: (value) => _handleGoalAction(goal, value),
                  ),
              ],
            ),
            const SizedBox(height: 8),
            Text(
              '${goal.currentValue.toStringAsFixed(1)} / ${goal.targetValue.toStringAsFixed(1)} ${goal.unit ?? ''}',
              style: const TextStyle(fontSize: 16),
            ),
            const SizedBox(height: 8),
            LinearProgressIndicator(
              value: progress / 100,
              backgroundColor: Colors.grey[300],
              valueColor: AlwaysStoppedAnimation<Color>(
                progress >= 100 ? Colors.green : Colors.blue,
              ),
            ),
            const SizedBox(height: 4),
            Text(
              '${progress.toStringAsFixed(1)}% Complete',
              style: const TextStyle(fontSize: 12, color: Colors.grey),
            ),
            if (goal.targetDate != null) ...[
              const SizedBox(height: 8),
              Text(
                'Target: ${dateFormat.format(goal.targetDate!)}',
                style: const TextStyle(fontSize: 12, color: Colors.grey),
              ),
            ],
          ],
        ),
      ),
    );
  }

  String _formatGoalType(String goalType) {
    // Convert camelCase to Title Case with spaces
    return goalType
        .replaceAllMapped(RegExp(r'([A-Z])'), (match) => ' ${match.group(0)}')
        .trim();
  }

  void _handleGoalAction(Goal goal, String action) async {
    final provider = context.read<GoalsProvider>();

    switch (action) {
      case 'progress':
        _showAddProgressDialog(goal);
        break;
      case 'complete':
        final confirmed = await _showConfirmDialog(
          'Complete Goal',
          'Mark this goal as completed?',
        );
        if (confirmed == true && mounted) {
          await provider.completeGoal(goal.id);
        }
        break;
      case 'delete':
        final confirmed = await _showConfirmDialog(
          'Delete Goal',
          'Are you sure you want to delete this goal?',
        );
        if (confirmed == true && mounted) {
          await provider.deleteGoal(goal.id);
        }
        break;
    }
  }

  Future<bool?> _showConfirmDialog(String title, String message) {
    return showDialog<bool>(
      context: context,
      builder:
          (context) => AlertDialog(
            title: Text(title),
            content: Text(message),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(context, false),
                child: const Text('Cancel'),
              ),
              ElevatedButton(
                onPressed: () => Navigator.pop(context, true),
                child: const Text('Confirm'),
              ),
            ],
          ),
    );
  }

  void _showCreateGoalDialog(BuildContext context) {
    showDialog(
      context: context,
      builder: (context) => const CreateGoalDialog(),
    );
  }

  void _showAddProgressDialog(Goal goal) {
    showDialog(
      context: context,
      builder: (context) => AddProgressDialog(goal: goal),
    );
  }
}

/// Dialog for creating a new goal
class CreateGoalDialog extends StatefulWidget {
  const CreateGoalDialog({super.key});

  @override
  State<CreateGoalDialog> createState() => _CreateGoalDialogState();
}

class _CreateGoalDialogState extends State<CreateGoalDialog> {
  final _formKey = GlobalKey<FormState>();
  final _goalTypeController = TextEditingController();
  final _targetValueController = TextEditingController();
  final _currentValueController = TextEditingController();
  final _unitController = TextEditingController();

  String _timeFrame = 'weekly';
  DateTime? _targetDate;
  bool _isCreating = false;

  @override
  void dispose() {
    _goalTypeController.dispose();
    _targetValueController.dispose();
    _currentValueController.dispose();
    _unitController.dispose();
    super.dispose();
  }

  Future<void> _selectTargetDate() async {
    final DateTime? picked = await showDatePicker(
      context: context,
      initialDate: DateTime.now().add(const Duration(days: 30)),
      firstDate: DateTime.now(),
      lastDate: DateTime.now().add(const Duration(days: 365)),
      helpText: 'Select Target Date',
    );

    if (picked != null) {
      setState(() {
        _targetDate = picked;
      });
    }
  }

  Future<void> _createGoal() async {
    if (!_formKey.currentState!.validate()) {
      return;
    }

    setState(() => _isCreating = true);

    final goal = Goal(
      id: 0, // Will be assigned by backend
      userId: 0, // Will be assigned by backend
      goalType: _goalTypeController.text.trim(),
      targetValue: double.parse(_targetValueController.text),
      currentValue: double.tryParse(_currentValueController.text) ?? 0,
      unit: _unitController.text.trim(),
      timeFrame: _timeFrame,
      startDate: DateTime.now(),
      targetDate: _targetDate,
      isActive: true,
      isCompleted: false,
      createdAt: DateTime.now(),
    );

    final provider = context.read<GoalsProvider>();
    final success = await provider.createGoal(goal);

    if (mounted) {
      setState(() => _isCreating = false);

      if (success) {
        Navigator.pop(context);
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Goal created successfully')),
        );
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(provider.errorMessage ?? 'Failed to create goal'),
            backgroundColor: Colors.red,
          ),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: const Text('Create Goal'),
      content: Form(
        key: _formKey,
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              TextFormField(
                controller: _goalTypeController,
                decoration: const InputDecoration(
                  labelText: 'Goal Type',
                  hintText: 'e.g., Weight, BodyFat, WorkoutFrequency',
                  prefixIcon: Icon(Icons.flag),
                ),
                validator: (value) {
                  if (value == null || value.trim().isEmpty) {
                    return 'Please enter a goal type';
                  }
                  return null;
                },
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _targetValueController,
                decoration: const InputDecoration(
                  labelText: 'Target Value',
                  prefixIcon: Icon(Icons.track_changes),
                ),
                keyboardType: const TextInputType.numberWithOptions(
                  decimal: true,
                ),
                validator: (value) {
                  if (value == null || value.trim().isEmpty) {
                    return 'Please enter a target value';
                  }
                  if (double.tryParse(value) == null) {
                    return 'Please enter a valid number';
                  }
                  return null;
                },
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _currentValueController,
                decoration: const InputDecoration(
                  labelText: 'Current Value (optional)',
                  prefixIcon: Icon(Icons.trending_up),
                ),
                keyboardType: const TextInputType.numberWithOptions(
                  decimal: true,
                ),
              ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _unitController,
                decoration: const InputDecoration(
                  labelText: 'Unit',
                  hintText: 'e.g., kg, %, workouts',
                  prefixIcon: Icon(Icons.straighten),
                ),
                validator: (value) {
                  if (value == null || value.trim().isEmpty) {
                    return 'Please enter a unit';
                  }
                  return null;
                },
              ),
              const SizedBox(height: 16),
              DropdownButtonFormField<String>(
                value: _timeFrame,
                decoration: const InputDecoration(
                  labelText: 'Time Frame',
                  prefixIcon: Icon(Icons.calendar_today),
                ),
                items: const [
                  DropdownMenuItem(value: 'daily', child: Text('Daily')),
                  DropdownMenuItem(value: 'weekly', child: Text('Weekly')),
                  DropdownMenuItem(value: 'monthly', child: Text('Monthly')),
                  DropdownMenuItem(value: 'yearly', child: Text('Yearly')),
                ],
                onChanged: (value) {
                  if (value != null) {
                    setState(() => _timeFrame = value);
                  }
                },
              ),
              const SizedBox(height: 16),
              InkWell(
                onTap: _selectTargetDate,
                child: InputDecorator(
                  decoration: const InputDecoration(
                    labelText: 'Target Date (optional)',
                    prefixIcon: Icon(Icons.event),
                  ),
                  child: Text(
                    _targetDate != null
                        ? DateFormat('MMM d, y').format(_targetDate!)
                        : 'Select target date',
                    style: TextStyle(
                      color: _targetDate != null ? null : Colors.grey,
                    ),
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
      actions: [
        TextButton(
          onPressed: _isCreating ? null : () => Navigator.pop(context),
          child: const Text('Cancel'),
        ),
        ElevatedButton(
          onPressed: _isCreating ? null : _createGoal,
          child:
              _isCreating
                  ? const SizedBox(
                    width: 20,
                    height: 20,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  )
                  : const Text('Create'),
        ),
      ],
    );
  }
}

/// Dialog for adding progress to a goal
class AddProgressDialog extends StatefulWidget {
  final Goal goal;

  const AddProgressDialog({super.key, required this.goal});

  @override
  State<AddProgressDialog> createState() => _AddProgressDialogState();
}

class _AddProgressDialogState extends State<AddProgressDialog> {
  final _formKey = GlobalKey<FormState>();
  final _valueController = TextEditingController();
  final _notesController = TextEditingController();
  bool _isAdding = false;

  @override
  void dispose() {
    _valueController.dispose();
    _notesController.dispose();
    super.dispose();
  }

  Future<void> _addProgress() async {
    if (!_formKey.currentState!.validate()) {
      return;
    }

    setState(() => _isAdding = true);

    final progress = GoalProgress(
      id: 0,
      goalId: widget.goal.id,
      recordedAt: DateTime.now(),
      value: double.parse(_valueController.text),
      notes:
          _notesController.text.trim().isEmpty
              ? null
              : _notesController.text.trim(),
    );

    final provider = context.read<GoalsProvider>();
    final success = await provider.addProgress(widget.goal.id, progress);

    if (mounted) {
      setState(() => _isAdding = false);

      if (success) {
        Navigator.pop(context);
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Progress added successfully')),
        );
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(provider.errorMessage ?? 'Failed to add progress'),
            backgroundColor: Colors.red,
          ),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: const Text('Add Progress'),
      content: Form(
        key: _formKey,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              widget.goal.goalType,
              style: const TextStyle(fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 8),
            Text(
              'Current: ${widget.goal.currentValue.toStringAsFixed(1)} ${widget.goal.unit ?? ''}',
              style: const TextStyle(color: Colors.grey),
            ),
            const SizedBox(height: 16),
            TextFormField(
              controller: _valueController,
              decoration: InputDecoration(
                labelText: 'New Value',
                suffixText: widget.goal.unit,
                prefixIcon: const Icon(Icons.edit),
              ),
              keyboardType: const TextInputType.numberWithOptions(
                decimal: true,
              ),
              autofocus: true,
              validator: (value) {
                if (value == null || value.trim().isEmpty) {
                  return 'Please enter a value';
                }
                if (double.tryParse(value) == null) {
                  return 'Please enter a valid number';
                }
                return null;
              },
            ),
            const SizedBox(height: 16),
            TextFormField(
              controller: _notesController,
              decoration: const InputDecoration(
                labelText: 'Notes (optional)',
                prefixIcon: Icon(Icons.note),
                hintText: 'Add a note about your progress...',
              ),
              maxLines: 3,
            ),
          ],
        ),
      ),
      actions: [
        TextButton(
          onPressed: _isAdding ? null : () => Navigator.pop(context),
          child: const Text('Cancel'),
        ),
        ElevatedButton(
          onPressed: _isAdding ? null : _addProgress,
          child:
              _isAdding
                  ? const SizedBox(
                    width: 20,
                    height: 20,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  )
                  : const Text('Add'),
        ),
      ],
    );
  }
}
