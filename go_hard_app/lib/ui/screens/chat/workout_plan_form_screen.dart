import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../../providers/chat_provider.dart';
import '../../../routes/route_names.dart';
import '../../widgets/common/offline_banner.dart';

/// Form screen for generating AI workout plans
class WorkoutPlanFormScreen extends StatefulWidget {
  const WorkoutPlanFormScreen({super.key});

  @override
  State<WorkoutPlanFormScreen> createState() => _WorkoutPlanFormScreenState();
}

class _WorkoutPlanFormScreenState extends State<WorkoutPlanFormScreen> {
  final _formKey = GlobalKey<FormState>();
  final _goalController = TextEditingController();
  final _limitationsController = TextEditingController();

  String _experienceLevel = 'beginner';
  int _daysPerWeek = 3;
  String _equipment = 'full gym';

  @override
  void dispose() {
    _goalController.dispose();
    _limitationsController.dispose();
    super.dispose();
  }

  Future<void> _generatePlan() async {
    if (!_formKey.currentState!.validate()) return;

    final provider = context.read<ChatProvider>();

    // Capture context-dependent objects before async operation
    final navigator = Navigator.of(context);
    final scaffoldMessenger = ScaffoldMessenger.of(context);

    // Show loading dialog
    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (context) => const Center(child: CircularProgressIndicator()),
    );

    final conversation = await provider.generateWorkoutPlan(
      goal: _goalController.text.trim(),
      experienceLevel: _experienceLevel,
      daysPerWeek: _daysPerWeek,
      equipment: _equipment,
      limitations:
          _limitationsController.text.trim().isEmpty
              ? null
              : _limitationsController.text.trim(),
    );

    if (mounted) {
      navigator.pop(); // Close loading dialog

      if (conversation != null) {
        // Navigate to conversation screen
        navigator.pushReplacementNamed(
          RouteNames.chatConversation,
          arguments: conversation.id,
        );
      } else if (provider.errorMessage != null) {
        // Show error
        scaffoldMessenger.showSnackBar(
          SnackBar(
            content: Text(provider.errorMessage!),
            backgroundColor: Colors.red,
          ),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Generate Workout Plan'),
        centerTitle: true,
      ),
      body: Column(
        children: [
          const OfflineBanner(),
          Expanded(
            child: SingleChildScrollView(
              padding: const EdgeInsets.all(16),
              child: Form(
                key: _formKey,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text(
                      'Tell us about your fitness goals',
                      style: TextStyle(
                        fontSize: 18,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    const SizedBox(height: 24),

                    // Goal
                    TextFormField(
                      controller: _goalController,
                      decoration: const InputDecoration(
                        labelText: 'Fitness Goal',
                        hintText:
                            'e.g., Build muscle, Lose weight, Gain strength',
                        border: OutlineInputBorder(),
                      ),
                      validator: (value) {
                        if (value == null || value.trim().isEmpty) {
                          return 'Please enter your fitness goal';
                        }
                        return null;
                      },
                    ),
                    const SizedBox(height: 16),

                    // Experience Level
                    DropdownButtonFormField<String>(
                      value: _experienceLevel,
                      decoration: const InputDecoration(
                        labelText: 'Experience Level',
                        border: OutlineInputBorder(),
                      ),
                      items: const [
                        DropdownMenuItem(
                          value: 'beginner',
                          child: Text('Beginner'),
                        ),
                        DropdownMenuItem(
                          value: 'intermediate',
                          child: Text('Intermediate'),
                        ),
                        DropdownMenuItem(
                          value: 'advanced',
                          child: Text('Advanced'),
                        ),
                      ],
                      onChanged: (value) {
                        setState(() {
                          _experienceLevel = value!;
                        });
                      },
                    ),
                    const SizedBox(height: 16),

                    // Days Per Week
                    const Text(
                      'Days Per Week',
                      style: TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.w500,
                      ),
                    ),
                    const SizedBox(height: 8),
                    Row(
                      children: List.generate(7, (index) {
                        final day = index + 1;
                        final isSelected = _daysPerWeek == day;
                        return Expanded(
                          child: Padding(
                            padding: const EdgeInsets.symmetric(horizontal: 4),
                            child: ChoiceChip(
                              label: Text('$day'),
                              selected: isSelected,
                              onSelected: (selected) {
                                if (selected) {
                                  setState(() {
                                    _daysPerWeek = day;
                                  });
                                }
                              },
                            ),
                          ),
                        );
                      }),
                    ),
                    const SizedBox(height: 16),

                    // Equipment
                    DropdownButtonFormField<String>(
                      value: _equipment,
                      decoration: const InputDecoration(
                        labelText: 'Available Equipment',
                        border: OutlineInputBorder(),
                      ),
                      items: const [
                        DropdownMenuItem(
                          value: 'full gym',
                          child: Text('Full Gym'),
                        ),
                        DropdownMenuItem(
                          value: 'dumbbells only',
                          child: Text('Dumbbells Only'),
                        ),
                        DropdownMenuItem(
                          value: 'bodyweight',
                          child: Text('Bodyweight Only'),
                        ),
                        DropdownMenuItem(
                          value: 'home gym',
                          child: Text('Home Gym'),
                        ),
                      ],
                      onChanged: (value) {
                        setState(() {
                          _equipment = value!;
                        });
                      },
                    ),
                    const SizedBox(height: 16),

                    // Limitations
                    TextFormField(
                      controller: _limitationsController,
                      decoration: const InputDecoration(
                        labelText: 'Injuries or Limitations (Optional)',
                        hintText: 'e.g., Lower back pain, knee injury',
                        border: OutlineInputBorder(),
                      ),
                      maxLines: 3,
                    ),
                    const SizedBox(height: 24),

                    // Generate Button
                    SizedBox(
                      width: double.infinity,
                      child: Consumer<ChatProvider>(
                        builder: (context, provider, child) {
                          return ElevatedButton(
                            onPressed:
                                provider.isOffline ? null : _generatePlan,
                            style: ElevatedButton.styleFrom(
                              padding: const EdgeInsets.symmetric(vertical: 16),
                            ),
                            child: const Text(
                              'Generate Workout Plan',
                              style: TextStyle(fontSize: 16),
                            ),
                          );
                        },
                      ),
                    ),

                    const SizedBox(height: 16),

                    // Info card
                    Container(
                      padding: const EdgeInsets.all(12),
                      decoration: BoxDecoration(
                        color: Colors.blue[50],
                        borderRadius: BorderRadius.circular(8),
                        border: Border.all(color: Colors.blue[200]!),
                      ),
                      child: Row(
                        children: [
                          Icon(Icons.info_outline, color: Colors.blue[700]),
                          const SizedBox(width: 12),
                          Expanded(
                            child: Text(
                              'AI will create a personalized workout plan based on your inputs. You can chat with it to adjust the plan.',
                              style: TextStyle(
                                fontSize: 13,
                                color: Colors.blue[900],
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}
