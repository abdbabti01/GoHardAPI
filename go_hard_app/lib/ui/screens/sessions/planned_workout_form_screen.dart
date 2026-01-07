import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:provider/provider.dart';
import 'package:table_calendar/table_calendar.dart';
import '../../../providers/sessions_provider.dart';

/// Screen for creating planned workouts with a beautiful calendar UI
/// Supports both single and recurring planned workouts
class PlannedWorkoutFormScreen extends StatefulWidget {
  const PlannedWorkoutFormScreen({super.key});

  @override
  State<PlannedWorkoutFormScreen> createState() =>
      _PlannedWorkoutFormScreenState();
}

class _PlannedWorkoutFormScreenState extends State<PlannedWorkoutFormScreen> {
  final _formKey = GlobalKey<FormState>();
  final _nameController = TextEditingController();
  final _notesController = TextEditingController();
  final _durationController = TextEditingController();
  final _intervalController = TextEditingController();
  final _occurrencesController = TextEditingController();

  DateTime _selectedDate = DateTime.now();
  DateTime _focusedDay = DateTime.now();
  String? _selectedType;
  bool _isRecurring = false;
  String _frequency = 'daily';
  List<int> _selectedDays = [];
  String _endCondition = 'occurrences';
  DateTime? _endDate;
  bool _isLoading = false;

  final List<String> _workoutTypes = [
    'Strength',
    'Cardio',
    'Mixed',
    'Flexibility',
    'HIIT',
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
  void dispose() {
    _nameController.dispose();
    _notesController.dispose();
    _durationController.dispose();
    _intervalController.dispose();
    _occurrencesController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Plan Workout'),
        leading: IconButton(
          icon: const Icon(Icons.close),
          onPressed: () => Navigator.pop(context, false),
        ),
      ),
      body: Form(
        key: _formKey,
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Calendar Widget
              _buildCalendar(),
              const SizedBox(height: 24),

              // Workout Details Form
              _buildWorkoutDetailsSection(),
              const SizedBox(height: 24),

              // Recurring Toggle
              _buildRecurringToggle(),
              const SizedBox(height: 16),

              // Recurrence Options (conditional)
              if (_isRecurring) ...[
                _buildRecurrenceOptions(),
                const SizedBox(height: 24),
              ],

              // Create Button
              _buildCreateButton(),
              const SizedBox(height: 32),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildCalendar() {
    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);

    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.05),
            blurRadius: 10,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: TableCalendar(
        firstDay: today,
        lastDay: DateTime.now().add(const Duration(days: 365)),
        focusedDay: _focusedDay,
        selectedDayPredicate: (day) => isSameDay(_selectedDate, day),
        onDaySelected: (selectedDay, focusedDay) {
          final selected = DateTime(
            selectedDay.year,
            selectedDay.month,
            selectedDay.day,
          );
          // Only allow today or future dates
          if (selected.isAfter(today.subtract(const Duration(days: 1)))) {
            setState(() {
              _selectedDate = selected;
              _focusedDay = focusedDay;
            });
          }
        },
        onPageChanged: (focusedDay) {
          _focusedDay = focusedDay;
        },
        calendarFormat: CalendarFormat.month,
        headerStyle: HeaderStyle(
          formatButtonVisible: false,
          titleCentered: true,
          titleTextStyle: Theme.of(
            context,
          ).textTheme.titleLarge!.copyWith(fontWeight: FontWeight.bold),
          leftChevronIcon: Icon(
            Icons.chevron_left,
            color: Theme.of(context).colorScheme.primary,
          ),
          rightChevronIcon: Icon(
            Icons.chevron_right,
            color: Theme.of(context).colorScheme.primary,
          ),
        ),
        calendarStyle: CalendarStyle(
          // Today's style
          todayDecoration: BoxDecoration(
            border: Border.all(
              color: Theme.of(context).colorScheme.primary,
              width: 2,
            ),
            shape: BoxShape.circle,
          ),
          todayTextStyle: TextStyle(
            color: Theme.of(context).colorScheme.primary,
            fontWeight: FontWeight.bold,
          ),
          // Selected date style
          selectedDecoration: BoxDecoration(
            gradient: LinearGradient(
              colors: [
                Theme.of(context).colorScheme.primary,
                Theme.of(context).colorScheme.primary.withValues(alpha: 0.8),
              ],
            ),
            shape: BoxShape.circle,
          ),
          selectedTextStyle: const TextStyle(
            color: Colors.white,
            fontWeight: FontWeight.bold,
          ),
          // Past dates style
          disabledTextStyle: TextStyle(color: Colors.grey.shade300),
          // Default style
          defaultTextStyle: const TextStyle(color: Colors.black87),
          weekendTextStyle: const TextStyle(color: Colors.black87),
          outsideTextStyle: TextStyle(color: Colors.grey.shade400),
        ),
        enabledDayPredicate: (day) {
          final dayOnly = DateTime(day.year, day.month, day.day);
          return dayOnly.isAfter(today.subtract(const Duration(days: 1)));
        },
      ),
    );
  }

  Widget _buildWorkoutDetailsSection() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          'Workout Details',
          style: Theme.of(
            context,
          ).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 16),

        // Workout Name
        TextFormField(
          controller: _nameController,
          decoration: const InputDecoration(
            labelText: 'Workout Name *',
            hintText: 'e.g., Leg Day',
            border: OutlineInputBorder(),
            prefixIcon: Icon(Icons.edit),
          ),
          validator: (value) {
            if (value == null || value.isEmpty) {
              return 'Please enter a workout name';
            }
            return null;
          },
        ),
        const SizedBox(height: 16),

        // Workout Type
        DropdownButtonFormField<String>(
          value: _selectedType,
          decoration: const InputDecoration(
            labelText: 'Type',
            hintText: 'Select workout type',
            border: OutlineInputBorder(),
            prefixIcon: Icon(Icons.category),
          ),
          items:
              _workoutTypes.map((type) {
                return DropdownMenuItem(value: type, child: Text(type));
              }).toList(),
          onChanged: (value) => setState(() => _selectedType = value),
        ),
        const SizedBox(height: 16),

        // Estimated Duration
        TextFormField(
          controller: _durationController,
          decoration: const InputDecoration(
            labelText: 'Estimated Duration (minutes)',
            hintText: '60',
            border: OutlineInputBorder(),
            prefixIcon: Icon(Icons.timer),
            suffixText: 'min',
          ),
          keyboardType: TextInputType.number,
          inputFormatters: [FilteringTextInputFormatter.digitsOnly],
        ),
        const SizedBox(height: 16),

        // Notes
        TextFormField(
          controller: _notesController,
          decoration: const InputDecoration(
            labelText: 'Notes',
            hintText: 'Add any notes or reminders...',
            border: OutlineInputBorder(),
            prefixIcon: Icon(Icons.notes),
          ),
          maxLines: 3,
        ),
      ],
    );
  }

  Widget _buildRecurringToggle() {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      decoration: BoxDecoration(
        color: Theme.of(context).colorScheme.primary.withValues(alpha: 0.05),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Row(
        children: [
          Icon(Icons.repeat, color: Theme.of(context).colorScheme.primary),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Make Recurring',
                  style: Theme.of(
                    context,
                  ).textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w600),
                ),
                Text(
                  'Create multiple workouts on a schedule',
                  style: Theme.of(
                    context,
                  ).textTheme.bodySmall?.copyWith(color: Colors.grey),
                ),
              ],
            ),
          ),
          Switch(
            value: _isRecurring,
            onChanged: (value) => setState(() => _isRecurring = value),
          ),
        ],
      ),
    );
  }

  Widget _buildRecurrenceOptions() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          'Recurrence Pattern',
          style: Theme.of(
            context,
          ).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 16),

        // Frequency Selection
        _buildFrequencySelector(),
        const SizedBox(height: 16),

        // Weekly Days (conditional)
        if (_frequency == 'weekly') ...[
          _buildWeeklyDaysSelector(),
          const SizedBox(height: 16),
        ],

        // Custom Interval (conditional)
        if (_frequency == 'custom') ...[
          _buildCustomIntervalInput(),
          const SizedBox(height: 16),
        ],

        // End Condition
        _buildEndConditionSelector(),
      ],
    );
  }

  Widget _buildFrequencySelector() {
    return Column(
      children: [
        RadioListTile<String>(
          title: const Text('Daily'),
          subtitle: const Text('Every day'),
          value: 'daily',
          groupValue: _frequency,
          onChanged:
              (value) => setState(() {
                _frequency = value!;
                _selectedDays = [];
              }),
        ),
        RadioListTile<String>(
          title: const Text('Weekly'),
          subtitle: const Text('Specific days of the week'),
          value: 'weekly',
          groupValue: _frequency,
          onChanged: (value) => setState(() => _frequency = value!),
        ),
        RadioListTile<String>(
          title: const Text('Custom Interval'),
          subtitle: const Text('Every X days'),
          value: 'custom',
          groupValue: _frequency,
          onChanged:
              (value) => setState(() {
                _frequency = value!;
                _selectedDays = [];
              }),
        ),
      ],
    );
  }

  Widget _buildWeeklyDaysSelector() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.only(left: 16, bottom: 8),
          child: Text(
            'Select Days',
            style: Theme.of(
              context,
            ).textTheme.bodySmall?.copyWith(color: Colors.grey.shade700),
          ),
        ),
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
                  selectedColor: Theme.of(context).colorScheme.primary,
                  checkmarkColor: Colors.white,
                  labelStyle: TextStyle(
                    color: isSelected ? Colors.white : null,
                  ),
                );
              }).toList(),
        ),
      ],
    );
  }

  Widget _buildCustomIntervalInput() {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16),
      child: TextFormField(
        controller: _intervalController,
        decoration: const InputDecoration(
          labelText: 'Repeat every X days',
          hintText: '3',
          border: OutlineInputBorder(),
          suffixText: 'days',
        ),
        keyboardType: TextInputType.number,
        inputFormatters: [FilteringTextInputFormatter.digitsOnly],
        validator: (value) {
          if (_isRecurring && _frequency == 'custom') {
            if (value == null || value.isEmpty) {
              return 'Please enter an interval';
            }
            final interval = int.tryParse(value);
            if (interval == null || interval < 1) {
              return 'Interval must be at least 1 day';
            }
          }
          return null;
        },
      ),
    );
  }

  Widget _buildEndConditionSelector() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          'End Condition',
          style: Theme.of(
            context,
          ).textTheme.bodyMedium?.copyWith(fontWeight: FontWeight.w600),
        ),
        const SizedBox(height: 8),
        RadioListTile<String>(
          title: const Text('After number of workouts'),
          value: 'occurrences',
          groupValue: _endCondition,
          onChanged: (value) => setState(() => _endCondition = value!),
        ),
        if (_endCondition == 'occurrences')
          Padding(
            padding: const EdgeInsets.only(left: 16, right: 16, bottom: 8),
            child: TextFormField(
              controller: _occurrencesController,
              decoration: const InputDecoration(
                labelText: 'Number of workouts',
                hintText: '12',
                border: OutlineInputBorder(),
                suffixText: 'workouts',
              ),
              keyboardType: TextInputType.number,
              inputFormatters: [FilteringTextInputFormatter.digitsOnly],
              validator: (value) {
                if (_isRecurring && _endCondition == 'occurrences') {
                  if (value == null || value.isEmpty) {
                    return 'Please enter number of workouts';
                  }
                  final count = int.tryParse(value);
                  if (count == null || count < 1) {
                    return 'Must be at least 1 workout';
                  }
                  if (count > 52) {
                    return 'Maximum 52 workouts allowed';
                  }
                }
                return null;
              },
            ),
          ),
        RadioListTile<String>(
          title: const Text('Until date'),
          value: 'until_date',
          groupValue: _endCondition,
          onChanged: (value) => setState(() => _endCondition = value!),
        ),
        if (_endCondition == 'until_date')
          Padding(
            padding: const EdgeInsets.only(left: 16, right: 16, bottom: 8),
            child: ListTile(
              title: Text(
                _endDate == null
                    ? 'Select end date'
                    : 'Until ${_endDate!.year}-${_endDate!.month.toString().padLeft(2, '0')}-${_endDate!.day.toString().padLeft(2, '0')}',
              ),
              trailing: const Icon(Icons.calendar_today),
              onTap: () => _selectEndDate(),
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(8),
                side: BorderSide(color: Colors.grey.shade300),
              ),
            ),
          ),
      ],
    );
  }

  Future<void> _selectEndDate() async {
    final maxDate = _selectedDate.add(const Duration(days: 365));

    final date = await showDatePicker(
      context: context,
      initialDate: _endDate ?? _selectedDate.add(const Duration(days: 30)),
      firstDate: _selectedDate.add(const Duration(days: 1)),
      lastDate: maxDate,
    );

    if (date != null) {
      setState(() => _endDate = date);
    }
  }

  Widget _buildCreateButton() {
    return SizedBox(
      width: double.infinity,
      height: 56,
      child: Container(
        decoration: BoxDecoration(
          borderRadius: BorderRadius.circular(28),
          gradient: LinearGradient(
            colors: [
              Theme.of(context).colorScheme.primary,
              Theme.of(context).colorScheme.primary.withValues(alpha: 0.8),
            ],
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
            borderRadius: BorderRadius.circular(28),
            onTap: _isLoading ? null : _handleCreate,
            child: Center(
              child:
                  _isLoading
                      ? const SizedBox(
                        height: 24,
                        width: 24,
                        child: CircularProgressIndicator(
                          color: Colors.white,
                          strokeWidth: 2,
                        ),
                      )
                      : Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(
                            _isRecurring ? Icons.repeat : Icons.check,
                            color: Colors.white,
                          ),
                          const SizedBox(width: 8),
                          Text(
                            _isRecurring ? 'Create Workouts' : 'Create Workout',
                            style: const TextStyle(
                              color: Colors.white,
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
  }

  Future<void> _handleCreate() async {
    if (!_formKey.currentState!.validate()) {
      return;
    }

    // Additional validation for recurring workouts
    if (_isRecurring) {
      if (_frequency == 'weekly' && _selectedDays.isEmpty) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Please select at least one day')),
        );
        return;
      }

      if (_endCondition == 'until_date' && _endDate == null) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Please select an end date')),
        );
        return;
      }
    }

    setState(() => _isLoading = true);

    try {
      final provider = context.read<SessionsProvider>();
      final name = _nameController.text.trim();
      final type = _selectedType;
      final notes = _notesController.text.trim();
      final duration = int.tryParse(_durationController.text);

      if (_isRecurring) {
        // Create recurring workouts
        final occurrences =
            _endCondition == 'occurrences'
                ? int.tryParse(_occurrencesController.text)
                : null;
        final intervalDays =
            _frequency == 'custom'
                ? int.tryParse(_intervalController.text)
                : null;

        final sessions = await provider.createRecurringPlannedWorkouts(
          name: name,
          startDate: _selectedDate,
          frequency: _frequency,
          daysOfWeek: _frequency == 'weekly' ? _selectedDays : null,
          intervalDays: intervalDays,
          occurrences: occurrences,
          endDate: _endCondition == 'until_date' ? _endDate : null,
          type: type,
          notes: notes.isEmpty ? null : notes,
          estimatedDuration: duration,
        );

        if (sessions.isNotEmpty && mounted) {
          HapticFeedback.mediumImpact();
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text(
                '${sessions.length} workouts scheduled successfully',
              ),
              backgroundColor: Colors.green,
            ),
          );
          Navigator.pop(context, true);
        }
      } else {
        // Create single planned workout
        final session = await provider.createPlannedWorkout(
          name: name,
          scheduledDate: _selectedDate,
          type: type,
          notes: notes.isEmpty ? null : notes,
          estimatedDuration: duration,
        );

        if (session != null && mounted) {
          HapticFeedback.mediumImpact();
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text(
                'Workout scheduled for ${_selectedDate.year}-${_selectedDate.month.toString().padLeft(2, '0')}-${_selectedDate.day.toString().padLeft(2, '0')}',
              ),
              backgroundColor: Colors.green,
            ),
          );
          Navigator.pop(context, true);
        }
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Error: ${e.toString()}'),
            backgroundColor: Colors.red,
          ),
        );
      }
    } finally {
      if (mounted) {
        setState(() => _isLoading = false);
      }
    }
  }
}
