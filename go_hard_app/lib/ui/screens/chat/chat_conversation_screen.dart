import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:flutter_markdown/flutter_markdown.dart';
import '../../../providers/chat_provider.dart';
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
      scaffoldMessenger.showSnackBar(
        SnackBar(
          content: Text(
            'Created $sessionsCount sessions ($matchedCount exercises matched)!',
          ),
          backgroundColor: Colors.green,
          duration: const Duration(seconds: 4),
          action: SnackBarAction(
            label: 'View',
            textColor: Colors.white,
            onPressed: () {
              navigator.popUntil((route) => route.isFirst);
            },
          ),
        ),
      );
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
