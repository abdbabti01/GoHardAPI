import 'package:flutter/material.dart';
import '../../../data/models/workout_template.dart';
import '../../../providers/workout_template_provider.dart';
import 'package:provider/provider.dart';

/// Dialog for creating or editing a workout template
class TemplateFormDialog extends StatefulWidget {
  final WorkoutTemplate? template; // null for create, non-null for edit

  const TemplateFormDialog({super.key, this.template});

  @override
  State<TemplateFormDialog> createState() => _TemplateFormDialogState();
}

class _TemplateFormDialogState extends State<TemplateFormDialog> {
  final _formKey = GlobalKey<FormState>();
  late TextEditingController _nameController;
  late TextEditingController _descriptionController;
  late TextEditingController _durationController;

  String _recurrencePattern = 'daily';
  String? _category;
  List<int> _selectedDays = [];
  int? _intervalDays;
  bool _isActive = true;

  final List<String> _categories = [
    'Strength',
    'Cardio',
    'Flexibility',
    'HIIT',
    'CrossFit',
    'Yoga',
    'Sports',
  ];

  final Map<int, String> _daysOfWeek = {
    1: 'Mon',
    2: 'Tue',
    3: 'Wed',
    4: 'Thu',
    5: 'Fri',
    6: 'Sat',
    7: 'Sun',
  };

  @override
  void initState() {
    super.initState();

    // Initialize controllers with existing data if editing
    _nameController = TextEditingController(text: widget.template?.name ?? '');
    _descriptionController = TextEditingController(
      text: widget.template?.description ?? '',
    );
    _durationController = TextEditingController(
      text: widget.template?.estimatedDuration?.toString() ?? '',
    );

    if (widget.template != null) {
      _recurrencePattern = widget.template!.recurrencePattern;
      _category = widget.template!.category;
      _isActive = widget.template!.isActive;
      _selectedDays = widget.template!.daysOfWeekList;
      _intervalDays = widget.template!.intervalDays;
    }
  }

  @override
  void dispose() {
    _nameController.dispose();
    _descriptionController.dispose();
    _durationController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final isEditing = widget.template != null;

    return AlertDialog(
      title: Text(isEditing ? 'Edit Template' : 'Create Template'),
      content: SingleChildScrollView(
        child: Form(
          key: _formKey,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Name field
              TextFormField(
                controller: _nameController,
                decoration: const InputDecoration(
                  labelText: 'Template Name',
                  hintText: 'e.g., Morning Workout',
                ),
                validator: (value) {
                  if (value == null || value.isEmpty) {
                    return 'Please enter a name';
                  }
                  return null;
                },
              ),
              const SizedBox(height: 16),

              // Description field
              TextFormField(
                controller: _descriptionController,
                decoration: const InputDecoration(
                  labelText: 'Description (optional)',
                  hintText: 'Brief description',
                ),
                maxLines: 2,
              ),
              const SizedBox(height: 16),

              // Category dropdown
              DropdownButtonFormField<String>(
                value: _category,
                decoration: const InputDecoration(labelText: 'Category'),
                items:
                    _categories.map((cat) {
                      return DropdownMenuItem(value: cat, child: Text(cat));
                    }).toList(),
                onChanged: (value) => setState(() => _category = value),
                validator: (value) {
                  if (value == null) return 'Please select a category';
                  return null;
                },
              ),
              const SizedBox(height: 16),

              // Estimated duration
              TextFormField(
                controller: _durationController,
                decoration: const InputDecoration(
                  labelText: 'Estimated Duration (minutes)',
                  hintText: '30',
                ),
                keyboardType: TextInputType.number,
              ),
              const SizedBox(height: 16),

              // Recurrence pattern
              const Text(
                'Recurrence Pattern',
                style: TextStyle(fontWeight: FontWeight.bold),
              ),
              const SizedBox(height: 8),

              RadioListTile<String>(
                title: const Text('Daily'),
                value: 'daily',
                groupValue: _recurrencePattern,
                onChanged: (value) {
                  setState(() {
                    _recurrencePattern = value!;
                    _selectedDays = [];
                    _intervalDays = null;
                  });
                },
              ),

              RadioListTile<String>(
                title: const Text('Weekly (specific days)'),
                value: 'weekly',
                groupValue: _recurrencePattern,
                onChanged: (value) {
                  setState(() {
                    _recurrencePattern = value!;
                    _intervalDays = null;
                  });
                },
              ),

              if (_recurrencePattern == 'weekly') ...[
                const SizedBox(height: 8),
                Wrap(
                  spacing: 8,
                  children:
                      _daysOfWeek.entries.map((entry) {
                        final isSelected = _selectedDays.contains(entry.key);
                        return FilterChip(
                          label: Text(entry.value),
                          selected: isSelected,
                          onSelected: (selected) {
                            setState(() {
                              if (selected) {
                                _selectedDays.add(entry.key);
                                _selectedDays.sort();
                              } else {
                                _selectedDays.remove(entry.key);
                              }
                            });
                          },
                        );
                      }).toList(),
                ),
              ],

              RadioListTile<String>(
                title: const Text('Custom interval'),
                value: 'custom',
                groupValue: _recurrencePattern,
                onChanged: (value) {
                  setState(() {
                    _recurrencePattern = value!;
                    _selectedDays = [];
                  });
                },
              ),

              if (_recurrencePattern == 'custom') ...[
                const SizedBox(height: 8),
                TextFormField(
                  decoration: const InputDecoration(
                    labelText: 'Every X days',
                    hintText: '3',
                  ),
                  keyboardType: TextInputType.number,
                  initialValue: _intervalDays?.toString(),
                  onChanged: (value) {
                    _intervalDays = int.tryParse(value);
                  },
                  validator: (value) {
                    if (_recurrencePattern == 'custom' &&
                        (value == null || value.isEmpty)) {
                      return 'Please enter interval days';
                    }
                    return null;
                  },
                ),
              ],

              const SizedBox(height: 16),

              // Active switch
              SwitchListTile(
                title: const Text('Active'),
                subtitle: const Text('Schedule this template'),
                value: _isActive,
                onChanged: (value) => setState(() => _isActive = value),
              ),
            ],
          ),
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.pop(context),
          child: const Text('Cancel'),
        ),
        ElevatedButton(
          onPressed: _saveTemplate,
          child: Text(isEditing ? 'Update' : 'Create'),
        ),
      ],
    );
  }

  Future<void> _saveTemplate() async {
    if (!_formKey.currentState!.validate()) return;

    // Validate weekly selection
    if (_recurrencePattern == 'weekly' && _selectedDays.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Please select at least one day')),
      );
      return;
    }

    final provider = context.read<WorkoutTemplateProvider>();
    final messenger = ScaffoldMessenger.of(context);
    final navigator = Navigator.of(context);

    // For now, use a simple exercise JSON placeholder
    // In a real app, you'd have a separate exercise builder UI
    final exercisesJson = '[{"name":"Exercise 1","sets":3,"reps":10}]';

    try {
      if (widget.template == null) {
        // Create new template
        final template = await provider.createTemplate(
          name: _nameController.text,
          description:
              _descriptionController.text.isEmpty
                  ? null
                  : _descriptionController.text,
          exercisesJson: exercisesJson,
          recurrencePattern: _recurrencePattern,
          daysOfWeek: _selectedDays.isEmpty ? null : _selectedDays.join(','),
          intervalDays: _intervalDays,
          estimatedDuration: int.tryParse(_durationController.text),
          category: _category,
        );

        if (template != null && mounted) {
          navigator.pop();
          messenger.showSnackBar(
            const SnackBar(content: Text('Template created successfully')),
          );
        }
      } else {
        // Update existing template
        final updatedTemplate = widget.template!.copyWith(
          name: _nameController.text,
          description:
              _descriptionController.text.isEmpty
                  ? null
                  : _descriptionController.text,
          recurrencePattern: _recurrencePattern,
          daysOfWeek: _selectedDays.isEmpty ? null : _selectedDays.join(','),
          intervalDays: _intervalDays,
          estimatedDuration: int.tryParse(_durationController.text),
          category: _category,
          isActive: _isActive,
        );

        final success = await provider.updateTemplate(updatedTemplate);
        if (success && mounted) {
          navigator.pop();
          messenger.showSnackBar(
            const SnackBar(content: Text('Template updated successfully')),
          );
        }
      }
    } catch (e) {
      if (mounted) {
        messenger.showSnackBar(
          SnackBar(content: Text('Error: ${e.toString()}')),
        );
      }
    }
  }
}
