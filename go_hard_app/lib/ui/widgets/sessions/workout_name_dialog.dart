import 'package:flutter/material.dart';
import '../../../core/constants/workout_names.dart';

/// Dialog for selecting a workout name from predefined options
class WorkoutNameDialog extends StatefulWidget {
  const WorkoutNameDialog({super.key});

  @override
  State<WorkoutNameDialog> createState() => _WorkoutNameDialogState();
}

class _WorkoutNameDialogState extends State<WorkoutNameDialog> {
  String? _selectedName;
  final TextEditingController _customNameController = TextEditingController();
  bool _useCustomName = false;

  @override
  void dispose() {
    _customNameController.dispose();
    super.dispose();
  }

  void _handleSelection(String name) {
    setState(() {
      _selectedName = name;
      _useCustomName = false;
    });
  }

  void _handleCustomNameToggle() {
    setState(() {
      _useCustomName = !_useCustomName;
      if (_useCustomName) {
        _selectedName = null;
      }
    });
  }

  void _handleConfirm() {
    if (_useCustomName) {
      final customName = _customNameController.text.trim();
      if (customName.isNotEmpty) {
        Navigator.of(context).pop(customName);
      }
    } else if (_selectedName != null) {
      Navigator.of(context).pop(_selectedName);
    }
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: const Text('Choose Workout Type'),
      content: SizedBox(
        width: double.maxFinite,
        child: ListView(
          shrinkWrap: true,
          children: [
            // Grouped workout names
            ...WorkoutNames.groupedWorkoutNames.entries.expand((entry) {
              return [
                // Group header
                Padding(
                  padding: const EdgeInsets.only(top: 16, bottom: 8),
                  child: Text(
                    entry.key,
                    style: Theme.of(context).textTheme.titleSmall?.copyWith(
                      fontWeight: FontWeight.bold,
                      color: Theme.of(context).colorScheme.primary,
                    ),
                  ),
                ),
                // Workout options in this group
                ...entry.value.map(
                  (name) => RadioListTile<String>(
                    title: Text(name),
                    value: name,
                    groupValue: _selectedName,
                    onChanged:
                        _useCustomName
                            ? null
                            : (value) {
                              if (value != null) _handleSelection(value);
                            },
                    dense: true,
                    contentPadding: EdgeInsets.zero,
                  ),
                ),
              ];
            }),

            const SizedBox(height: 16),
            const Divider(),

            // Custom name option
            SwitchListTile(
              title: const Text('Custom Name'),
              value: _useCustomName,
              onChanged: (value) => _handleCustomNameToggle(),
              contentPadding: EdgeInsets.zero,
            ),

            if (_useCustomName) ...[
              const SizedBox(height: 8),
              TextField(
                controller: _customNameController,
                decoration: const InputDecoration(
                  labelText: 'Enter custom workout name',
                  border: OutlineInputBorder(),
                  hintText: 'e.g., Upper Body Day',
                ),
                autofocus: true,
                textCapitalization: TextCapitalization.words,
              ),
            ],
          ],
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: const Text('Cancel'),
        ),
        ElevatedButton(
          onPressed:
              _useCustomName
                  ? (_customNameController.text.trim().isNotEmpty
                      ? _handleConfirm
                      : null)
                  : (_selectedName != null ? _handleConfirm : null),
          child: const Text('Start Workout'),
        ),
      ],
    );
  }
}
