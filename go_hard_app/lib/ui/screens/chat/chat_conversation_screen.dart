import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:flutter_markdown/flutter_markdown.dart';
import '../../../providers/chat_provider.dart';
import '../../../providers/sessions_provider.dart';
import '../../../providers/active_workout_provider.dart';
import '../../../data/models/exercise_template.dart';
import '../../../data/services/api_service.dart';
import '../../../core/constants/api_config.dart';
import '../../widgets/common/offline_banner.dart';

/// Chat conversation screen showing messages and input
class ChatConversationScreen extends StatefulWidget {
  final int conversationId;

  const ChatConversationScreen({super.key, required this.conversationId});

  @override
  State<ChatConversationScreen> createState() => _ChatConversationScreenState();
}

class _ChatConversationScreenState extends State<ChatConversationScreen> {
  final TextEditingController _messageController = TextEditingController();
  final ScrollController _scrollController = ScrollController();

  @override
  void initState() {
    super.initState();
    // Load conversation on first build
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<ChatProvider>().loadConversation(widget.conversationId);
    });
  }

  @override
  void dispose() {
    _messageController.dispose();
    _scrollController.dispose();
    super.dispose();
  }

  Future<void> _sendMessage() async {
    final message = _messageController.text.trim();
    if (message.isEmpty) return;

    // Clear input immediately
    _messageController.clear();

    // Send message
    final provider = context.read<ChatProvider>();
    final success = await provider.sendMessage(message);

    // Scroll to bottom after sending
    if (success && mounted) {
      _scrollToBottom();
    }
  }

  void _scrollToBottom() {
    if (_scrollController.hasClients) {
      Future.delayed(const Duration(milliseconds: 100), () {
        if (_scrollController.hasClients) {
          _scrollController.animateTo(
            _scrollController.position.maxScrollExtent,
            duration: const Duration(milliseconds: 300),
            curve: Curves.easeOut,
          );
        }
      });
    }
  }

  Future<void> _saveWorkoutPlan() async {
    final navigator = Navigator.of(context);
    final chatProvider = context.read<ChatProvider>();
    final scaffoldMessenger = ScaffoldMessenger.of(context);

    // Step 1: Show loading and fetch preview
    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (context) => const Center(child: CircularProgressIndicator()),
    );

    final preview = await chatProvider.previewSessionsFromPlan();

    if (!mounted) return;
    navigator.pop(); // Close loading

    if (preview == null) {
      scaffoldMessenger.showSnackBar(
        SnackBar(
          content: Text(chatProvider.errorMessage ?? 'Failed to load preview'),
          backgroundColor: Colors.red,
        ),
      );
      return;
    }

    // Step 2: Show preview dialog with start date picker
    final result = await _showPreviewDialog(preview);

    if (result == null || !mounted) return;

    // Step 3: Create sessions with chosen start date
    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (context) => const Center(child: CircularProgressIndicator()),
    );

    final createResult = await chatProvider.createSessionsFromPlan(
      startDate: result,
    );

    if (!mounted) return;
    navigator.pop(); // Close loading

    if (createResult != null) {
      final sessionsCount = createResult['sessions']?.length ?? 0;
      final matchedCount = createResult['matchedTemplates'] ?? 0;

      // Delete the conversation after successful session creation
      final conversationId = chatProvider.currentConversation?.id;
      if (conversationId != null) {
        await chatProvider.deleteConversation(conversationId);
      }

      // Refresh sessions list to show newly created sessions
      // Use waitForSync to ensure sessions are fully loaded from server
      if (mounted) {
        debugPrint('🔄 Refreshing sessions after creating workout plan...');
        await context.read<SessionsProvider>().loadSessions(
          showLoading: false,
          waitForSync: true,
        );
        debugPrint('✅ Sessions refreshed after workout plan creation');
      }

      // Navigate to sessions screen (tab 0) and refresh sessions
      if (mounted) {
        // Pop to main screen and switch to Sessions tab
        navigator.pushNamedAndRemoveUntil(
          '/main',
          (route) => false,
          arguments: 0, // Sessions tab index
        );

        // Show success message
        scaffoldMessenger.showSnackBar(
          SnackBar(
            content: Text(
              'Created $sessionsCount sessions ($matchedCount exercises matched)!',
            ),
            backgroundColor: Colors.green,
            duration: const Duration(seconds: 3),
          ),
        );
      }
    } else {
      scaffoldMessenger.showSnackBar(
        SnackBar(
          content: Text(
            chatProvider.errorMessage ?? 'Failed to create sessions',
          ),
          backgroundColor: Colors.red,
        ),
      );
    }
  }

  Future<DateTime?> _showPreviewDialog(Map<String, dynamic> preview) async {
    DateTime selectedDate = DateTime.now();
    final sessions = (preview['sessions'] as List?) ?? [];

    return showDialog<DateTime>(
      context: context,
      builder:
          (context) => StatefulBuilder(
            builder:
                (context, setState) => AlertDialog(
                  title: const Text('Preview Workout Plan'),
                  content: SingleChildScrollView(
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          '${sessions.length} sessions will be created:',
                          style: const TextStyle(fontWeight: FontWeight.bold),
                        ),
                        const SizedBox(height: 12),
                        ...sessions.map((session) {
                          final dayNum = session['dayNumber'] ?? 0;
                          final name = session['name'] ?? 'Session';
                          final exerciseCount = session['exerciseCount'] ?? 0;
                          return Padding(
                            padding: const EdgeInsets.only(bottom: 8),
                            child: Row(
                              children: [
                                CircleAvatar(
                                  radius: 16,
                                  child: Text('$dayNum'),
                                ),
                                const SizedBox(width: 12),
                                Expanded(
                                  child: Column(
                                    crossAxisAlignment:
                                        CrossAxisAlignment.start,
                                    children: [
                                      Text(
                                        name,
                                        style: const TextStyle(
                                          fontWeight: FontWeight.w500,
                                        ),
                                      ),
                                      Text(
                                        '$exerciseCount exercises',
                                        style: TextStyle(
                                          fontSize: 12,
                                          color: Colors.grey[600],
                                        ),
                                      ),
                                    ],
                                  ),
                                ),
                              ],
                            ),
                          );
                        }),
                        const Divider(height: 24),
                        const Text(
                          'Start Date:',
                          style: TextStyle(fontWeight: FontWeight.bold),
                        ),
                        const SizedBox(height: 8),
                        OutlinedButton.icon(
                          onPressed: () async {
                            final picked = await showDatePicker(
                              context: context,
                              initialDate: selectedDate,
                              firstDate: DateTime.now().subtract(
                                const Duration(days: 7),
                              ),
                              lastDate: DateTime.now().add(
                                const Duration(days: 365),
                              ),
                            );
                            if (picked != null) {
                              setState(() {
                                selectedDate = picked;
                              });
                            }
                          },
                          icon: const Icon(Icons.calendar_today),
                          label: Text(
                            '${selectedDate.month}/${selectedDate.day}/${selectedDate.year}',
                          ),
                        ),
                        const SizedBox(height: 8),
                        Text(
                          'Sessions will be spaced every 2 days',
                          style: TextStyle(
                            fontSize: 12,
                            color: Colors.grey[600],
                          ),
                        ),
                      ],
                    ),
                  ),
                  actions: [
                    TextButton(
                      onPressed: () => Navigator.pop(context),
                      child: const Text('Cancel'),
                    ),
                    ElevatedButton(
                      onPressed: () => Navigator.pop(context, selectedDate),
                      child: const Text('Create Sessions'),
                    ),
                  ],
                ),
          ),
    );
  }

  /// Detect if message contains workout pattern
  bool _detectWorkoutPattern(String message) {
    // Look for patterns like "4 sets", "x 8-10 reps", "3x10", etc.
    final hasSetPattern = RegExp(
      r'\d+\s*(sets?|x)',
      caseSensitive: false,
    ).hasMatch(message);
    final hasRepPattern = RegExp(
      r'(x\s*)?\d+(-\d+)?\s*(reps?|repetitions?)',
      caseSensitive: false,
    ).hasMatch(message);
    return hasSetPattern || hasRepPattern;
  }

  /// Parse exercise names from message
  List<String> _parseExerciseNames(String message) {
    final exercises = <String>[];
    final lines = message.split('\n');

    // Multiple regex patterns to try, in order of preference
    final patterns = [
      // Pattern 1: "1. Bench Press - 4 sets x 8-10 reps"
      RegExp(
        r'^\s*(?:\d+\.|\*|-)\s*([A-Za-z][A-Za-z\s\-]+?)\s*[-:]\s*\d+',
        caseSensitive: false,
      ),
      // Pattern 2: "Bench Press: 4x10"
      RegExp(r'^\s*([A-Za-z][A-Za-z\s\-]+?):\s*\d+', caseSensitive: false),
      // Pattern 3: "- Bench Press (4 sets)"
      RegExp(r'^\s*[-*]\s*([A-Za-z][A-Za-z\s\-]+?)\s*\(', caseSensitive: false),
      // Pattern 4: "**Bench Press**" or "Bench Press" at start of line
      RegExp(
        r'^\s*\*{0,2}([A-Z][A-Za-z\s\-]+?)\*{0,2}\s*[-:(]',
        caseSensitive: false,
      ),
      // Pattern 5: Just exercise name followed by numbers
      RegExp(r'^\s*(?:\d+\.|\*|-)?\s*([A-Z][A-Za-z\s\-]+?)\s+\d+'),
    ];

    for (final line in lines) {
      if (line.trim().isEmpty) continue;

      for (final pattern in patterns) {
        final match = pattern.firstMatch(line);
        if (match != null) {
          final name = match.group(1)?.trim() ?? '';
          // Clean up the name
          final cleanName =
              name
                  .replaceAll(RegExp(r'\s+'), ' ') // normalize spaces
                  .replaceAll(RegExp(r'[*_]'), '') // remove markdown
                  .trim();

          if (cleanName.isNotEmpty &&
              cleanName.length >= 3 &&
              cleanName.length < 50 &&
              !exercises.contains(cleanName)) {
            exercises.add(cleanName);
            debugPrint('✅ Parsed exercise: "$cleanName" from line: "$line"');
            break; // Found match, move to next line
          }
        }
      }
    }

    debugPrint('📋 Total exercises parsed: ${exercises.length}');
    if (exercises.isEmpty) {
      debugPrint('⚠️ No exercises found. Message preview:');
      debugPrint(
        message.substring(0, message.length > 200 ? 200 : message.length),
      );
    }

    return exercises;
  }

  /// Fetch all exercise templates from API
  Future<List<ExerciseTemplate>> _fetchExerciseTemplates() async {
    try {
      final apiService = context.read<ApiService>();
      final data = await apiService.get<List<dynamic>>(
        ApiConfig.exerciseTemplates,
      );
      return data
          .map(
            (json) => ExerciseTemplate.fromJson(json as Map<String, dynamic>),
          )
          .toList();
    } catch (e) {
      debugPrint('Failed to fetch exercise templates: $e');
      return [];
    }
  }

  /// Match parsed exercise names to templates
  List<int> _matchExercises(
    List<String> names,
    List<ExerciseTemplate> templates,
  ) {
    final matchedIds = <int>[];

    for (final name in names) {
      // Try exact match first
      var match = templates.firstWhere(
        (t) => t.name.toLowerCase() == name.toLowerCase(),
        orElse: () => ExerciseTemplate(id: 0, name: '', category: ''),
      );

      // If no exact match, try partial match
      if (match.id == 0) {
        match = templates.firstWhere(
          (t) =>
              t.name.toLowerCase().contains(name.toLowerCase()) ||
              name.toLowerCase().contains(t.name.toLowerCase()),
          orElse: () => ExerciseTemplate(id: 0, name: '', category: ''),
        );
      }

      if (match.id != 0) {
        matchedIds.add(match.id);
        debugPrint('✅ Matched: "$name" → ${match.name} (ID: ${match.id})');
      } else {
        debugPrint('⚠️ No match for: "$name"');
      }
    }

    return matchedIds;
  }

  /// Start workout from AI message
  Future<void> _startWorkoutFromAI(String message) async {
    final navigator = Navigator.of(context);
    final scaffoldMessenger = ScaffoldMessenger.of(context);
    final activeWorkoutProvider = context.read<ActiveWorkoutProvider>();

    // Show loading
    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (context) => const Center(child: CircularProgressIndicator()),
    );

    try {
      // Parse exercise names
      final exerciseNames = _parseExerciseNames(message);
      if (exerciseNames.isEmpty) {
        throw Exception('No exercises found in message');
      }

      debugPrint('Parsed ${exerciseNames.length} exercises: $exerciseNames');

      // Fetch templates
      final templates = await _fetchExerciseTemplates();
      if (templates.isEmpty) {
        throw Exception('Failed to load exercise templates');
      }

      // Match exercises
      final matchedIds = _matchExercises(exerciseNames, templates);
      if (matchedIds.isEmpty) {
        throw Exception('No matching exercises found');
      }

      // Create workout
      final sessionId = await activeWorkoutProvider.createWorkoutFromAI(
        workoutName: 'AI Generated Workout',
        exerciseTemplateIds: matchedIds,
      );

      if (!mounted) return;
      navigator.pop(); // Close loading

      if (sessionId != null) {
        // Navigate to active workout screen
        navigator.pushNamed('/active-workout', arguments: sessionId);

        scaffoldMessenger.showSnackBar(
          SnackBar(
            content: Text(
              'Created workout with ${matchedIds.length} exercises!',
            ),
            backgroundColor: Colors.green,
          ),
        );
      } else {
        throw Exception(
          activeWorkoutProvider.errorMessage ?? 'Failed to create workout',
        );
      }
    } catch (e) {
      if (!mounted) return;
      navigator.pop(); // Close loading

      scaffoldMessenger.showSnackBar(
        SnackBar(
          content: Text('Failed to start workout: $e'),
          backgroundColor: Colors.red,
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Consumer<ChatProvider>(
          builder: (context, provider, child) {
            return Text(
              provider.currentConversation?.title ?? 'Chat',
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
            );
          },
        ),
        centerTitle: true,
        actions: [
          Consumer<ChatProvider>(
            builder: (context, provider, child) {
              // Only show "Save to Workouts" button for workout plan conversations
              if (provider.currentConversation?.type != 'workout_plan') {
                return const SizedBox.shrink();
              }

              return IconButton(
                icon: const Icon(Icons.save),
                tooltip: 'Save to My Workouts',
                onPressed: provider.isOffline ? null : _saveWorkoutPlan,
              );
            },
          ),
        ],
      ),
      body: Column(
        children: [
          const OfflineBanner(),
          Expanded(
            child: Consumer<ChatProvider>(
              builder: (context, provider, child) {
                if (provider.isLoading &&
                    provider.currentConversation == null) {
                  return const Center(child: CircularProgressIndicator());
                }

                if (provider.errorMessage != null &&
                    provider.currentConversation == null) {
                  return Center(
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Text(
                          provider.errorMessage!,
                          style: const TextStyle(color: Colors.red),
                          textAlign: TextAlign.center,
                        ),
                        const SizedBox(height: 16),
                        ElevatedButton(
                          onPressed:
                              () => provider.loadConversation(
                                widget.conversationId,
                              ),
                          child: const Text('Retry'),
                        ),
                      ],
                    ),
                  );
                }

                final conversation = provider.currentConversation;
                if (conversation == null) {
                  return const Center(child: Text('Conversation not found'));
                }

                if (conversation.messages.isEmpty) {
                  return Center(
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Icon(
                          Icons.chat_bubble_outline,
                          size: 64,
                          color: Colors.grey[400],
                        ),
                        const SizedBox(height: 16),
                        Text(
                          'No messages yet',
                          style: TextStyle(
                            fontSize: 18,
                            color: Colors.grey[600],
                          ),
                        ),
                        const SizedBox(height: 8),
                        Text(
                          'Start the conversation below',
                          style: TextStyle(
                            fontSize: 14,
                            color: Colors.grey[500],
                          ),
                        ),
                      ],
                    ),
                  );
                }

                // Scroll to bottom when messages change
                WidgetsBinding.instance.addPostFrameCallback((_) {
                  _scrollToBottom();
                });

                return ListView.builder(
                  controller: _scrollController,
                  padding: const EdgeInsets.symmetric(
                    horizontal: 8,
                    vertical: 16,
                  ),
                  itemCount: conversation.messages.length,
                  itemBuilder: (context, index) {
                    final message = conversation.messages[index];
                    final isUser = message.role == 'user';

                    return Align(
                      alignment:
                          isUser ? Alignment.centerRight : Alignment.centerLeft,
                      child: Container(
                        margin: EdgeInsets.only(
                          bottom: 8,
                          left: isUser ? 48 : 0,
                          right: isUser ? 0 : 48,
                        ),
                        padding: const EdgeInsets.all(12),
                        decoration: BoxDecoration(
                          color:
                              isUser
                                  ? Theme.of(context).primaryColor
                                  : Colors.grey[200],
                          borderRadius: BorderRadius.circular(12),
                        ),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            if (isUser)
                              Text(
                                message.content,
                                style: const TextStyle(
                                  color: Colors.white,
                                  fontSize: 16,
                                ),
                              )
                            else
                              MarkdownBody(
                                data: message.content,
                                styleSheet: MarkdownStyleSheet(
                                  p: const TextStyle(
                                    color: Colors.black87,
                                    fontSize: 16,
                                  ),
                                  code: TextStyle(
                                    backgroundColor: Colors.grey[300],
                                    fontFamily: 'monospace',
                                  ),
                                  codeblockDecoration: BoxDecoration(
                                    color: Colors.grey[300],
                                    borderRadius: BorderRadius.circular(4),
                                  ),
                                ),
                              ),
                            // Show "Start This Workout" button if AI message contains workout
                            if (!isUser &&
                                _detectWorkoutPattern(message.content))
                              Padding(
                                padding: const EdgeInsets.only(top: 12),
                                child: ElevatedButton.icon(
                                  onPressed:
                                      () =>
                                          _startWorkoutFromAI(message.content),
                                  icon: const Icon(
                                    Icons.fitness_center,
                                    size: 18,
                                  ),
                                  label: const Text('Start This Workout'),
                                  style: ElevatedButton.styleFrom(
                                    backgroundColor:
                                        Theme.of(context).primaryColor,
                                    foregroundColor: Colors.white,
                                    padding: const EdgeInsets.symmetric(
                                      horizontal: 16,
                                      vertical: 8,
                                    ),
                                  ),
                                ),
                              ),
                            if (!isUser && message.model != null)
                              Padding(
                                padding: const EdgeInsets.only(top: 8),
                                child: Text(
                                  'Model: ${message.model}',
                                  style: TextStyle(
                                    fontSize: 10,
                                    color: Colors.grey[600],
                                  ),
                                ),
                              ),
                          ],
                        ),
                      ),
                    );
                  },
                );
              },
            ),
          ),
          Consumer<ChatProvider>(
            builder: (context, provider, child) {
              if (provider.isSending) {
                return Container(
                  padding: const EdgeInsets.all(16),
                  child: Row(
                    children: [
                      const SizedBox(
                        width: 20,
                        height: 20,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      ),
                      const SizedBox(width: 12),
                      Text(
                        'AI is thinking...',
                        style: TextStyle(color: Colors.grey[600], fontSize: 14),
                      ),
                    ],
                  ),
                );
              }

              return Container(
                padding: const EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: Colors.white,
                  boxShadow: [
                    BoxShadow(
                      color: Colors.black.withValues(alpha: 0.05),
                      blurRadius: 4,
                      offset: const Offset(0, -2),
                    ),
                  ],
                ),
                child: SafeArea(
                  child: Row(
                    children: [
                      Expanded(
                        child: TextField(
                          controller: _messageController,
                          decoration: InputDecoration(
                            hintText: 'Type a message...',
                            border: OutlineInputBorder(
                              borderRadius: BorderRadius.circular(24),
                            ),
                            contentPadding: const EdgeInsets.symmetric(
                              horizontal: 16,
                              vertical: 12,
                            ),
                          ),
                          maxLines: null,
                          textCapitalization: TextCapitalization.sentences,
                          onSubmitted: (value) => _sendMessage(),
                          enabled: !provider.isOffline,
                        ),
                      ),
                      const SizedBox(width: 8),
                      IconButton(
                        onPressed: provider.isOffline ? null : _sendMessage,
                        icon: const Icon(Icons.send),
                        color: Theme.of(context).primaryColor,
                      ),
                    ],
                  ),
                ),
              );
            },
          ),
        ],
      ),
    );
  }
}
