import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../../providers/chat_provider.dart';
import '../../../routes/route_names.dart';
import '../../widgets/common/offline_banner.dart';

/// Chat list screen displaying all AI conversations
class ChatListScreen extends StatefulWidget {
  const ChatListScreen({super.key});

  @override
  State<ChatListScreen> createState() => _ChatListScreenState();
}

class _ChatListScreenState extends State<ChatListScreen> {
  @override
  void initState() {
    super.initState();
    // Load conversations on first build
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<ChatProvider>().loadConversations();
    });
  }

  Future<void> _handleRefresh() async {
    await context.read<ChatProvider>().refresh();
  }

  void _handleConversationTap(int conversationId) {
    Navigator.of(
      context,
    ).pushNamed(RouteNames.chatConversation, arguments: conversationId);
  }

  void _showQuickActionsSheet() {
    showModalBottomSheet(
      context: context,
      builder:
          (context) => SafeArea(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                ListTile(
                  leading: const Icon(Icons.fitness_center),
                  title: const Text('Generate Workout Plan'),
                  onTap: () {
                    Navigator.pop(context);
                    Navigator.of(context).pushNamed(RouteNames.workoutPlanForm);
                  },
                ),
                ListTile(
                  leading: const Icon(Icons.restaurant),
                  title: const Text('Generate Meal Plan'),
                  onTap: () {
                    Navigator.pop(context);
                    Navigator.of(context).pushNamed(RouteNames.mealPlanForm);
                  },
                ),
                ListTile(
                  leading: const Icon(Icons.analytics),
                  title: const Text('Analyze My Progress'),
                  onTap: () async {
                    final navigator = Navigator.of(context);
                    navigator.pop();
                    final provider = context.read<ChatProvider>();

                    // Show loading dialog
                    showDialog(
                      context: context,
                      barrierDismissible: false,
                      builder:
                          (context) =>
                              const Center(child: CircularProgressIndicator()),
                    );

                    final conversation = await provider.analyzeProgress();

                    if (mounted) {
                      navigator.pop(); // Close loading dialog

                      if (conversation != null) {
                        navigator.pushNamed(
                          RouteNames.chatConversation,
                          arguments: conversation.id,
                        );
                      }
                    }
                  },
                ),
                ListTile(
                  leading: const Icon(Icons.chat),
                  title: const Text('New Chat'),
                  onTap: () async {
                    final navigator = Navigator.of(context);
                    navigator.pop();
                    final provider = context.read<ChatProvider>();
                    final conversation = await provider.createConversation(
                      title: 'New Chat',
                      type: 'general',
                    );

                    if (conversation != null && mounted) {
                      navigator.pushNamed(
                        RouteNames.chatConversation,
                        arguments: conversation.id,
                      );
                    }
                  },
                ),
              ],
            ),
          ),
    );
  }

  Future<void> _showDeleteAllConfirmation() async {
    final provider = context.read<ChatProvider>();

    if (provider.conversations.isEmpty) {
      return;
    }

    final confirmed = await showDialog<bool>(
      context: context,
      builder:
          (context) => AlertDialog(
            title: const Text('Delete All Conversations'),
            content: Text(
              'Are you sure you want to delete all ${provider.conversations.length} conversations? This action cannot be undone.',
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(context, false),
                child: const Text('Cancel'),
              ),
              TextButton(
                onPressed: () => Navigator.pop(context, true),
                style: TextButton.styleFrom(foregroundColor: Colors.red),
                child: const Text('Delete All'),
              ),
            ],
          ),
    );

    if (confirmed == true && mounted) {
      // Show loading
      showDialog(
        context: context,
        barrierDismissible: false,
        builder: (context) => const Center(child: CircularProgressIndicator()),
      );

      final success = await provider.deleteAllConversations();

      if (mounted) {
        Navigator.pop(context); // Close loading

        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              success
                  ? 'All conversations deleted'
                  : provider.errorMessage ?? 'Failed to delete conversations',
            ),
            backgroundColor: success ? Colors.green : Colors.red,
          ),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('AI Assistant'),
        centerTitle: true,
        actions: [
          Consumer<ChatProvider>(
            builder: (context, provider, child) {
              if (provider.conversations.isEmpty) {
                return const SizedBox.shrink();
              }
              return IconButton(
                icon: const Icon(Icons.delete_sweep),
                tooltip: 'Delete All',
                onPressed: _showDeleteAllConfirmation,
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
                if (provider.isLoading && provider.conversations.isEmpty) {
                  return const Center(child: CircularProgressIndicator());
                }

                if (provider.errorMessage != null) {
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
                          onPressed: () => provider.loadConversations(),
                          child: const Text('Retry'),
                        ),
                      ],
                    ),
                  );
                }

                if (provider.conversations.isEmpty) {
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
                          'No conversations yet',
                          style: TextStyle(
                            fontSize: 18,
                            color: Colors.grey[600],
                          ),
                        ),
                        const SizedBox(height: 8),
                        Text(
                          'Tap the + button to get started',
                          style: TextStyle(
                            fontSize: 14,
                            color: Colors.grey[500],
                          ),
                        ),
                      ],
                    ),
                  );
                }

                return RefreshIndicator(
                  onRefresh: _handleRefresh,
                  child: ListView.builder(
                    itemCount: provider.conversations.length,
                    itemBuilder: (context, index) {
                      final conversation = provider.conversations[index];
                      return Dismissible(
                        key: Key('conversation_${conversation.id}'),
                        direction: DismissDirection.endToStart,
                        background: Container(
                          color: Colors.red,
                          alignment: Alignment.centerRight,
                          padding: const EdgeInsets.only(right: 16),
                          child: const Icon(Icons.delete, color: Colors.white),
                        ),
                        confirmDismiss: (direction) async {
                          return await showDialog<bool>(
                            context: context,
                            builder:
                                (context) => AlertDialog(
                                  title: const Text('Delete Conversation'),
                                  content: const Text(
                                    'Are you sure you want to delete this conversation?',
                                  ),
                                  actions: [
                                    TextButton(
                                      onPressed:
                                          () => Navigator.pop(context, false),
                                      child: const Text('Cancel'),
                                    ),
                                    TextButton(
                                      onPressed:
                                          () => Navigator.pop(context, true),
                                      style: TextButton.styleFrom(
                                        foregroundColor: Colors.red,
                                      ),
                                      child: const Text('Delete'),
                                    ),
                                  ],
                                ),
                          );
                        },
                        onDismissed: (direction) {
                          provider.deleteConversation(conversation.id);
                        },
                        child: ListTile(
                          leading: CircleAvatar(
                            backgroundColor: _getTypeColor(conversation.type),
                            child: Icon(
                              _getTypeIcon(conversation.type),
                              color: Colors.white,
                            ),
                          ),
                          title: Text(
                            conversation.title,
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                          ),
                          subtitle: Text(
                            _getTypeLabel(conversation.type),
                            style: TextStyle(
                              fontSize: 12,
                              color: Colors.grey[600],
                            ),
                          ),
                          trailing: Text(
                            _formatDate(
                              conversation.lastMessageAt ??
                                  conversation.createdAt,
                            ),
                            style: TextStyle(
                              fontSize: 12,
                              color: Colors.grey[500],
                            ),
                          ),
                          onTap: () => _handleConversationTap(conversation.id),
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
      floatingActionButton: FloatingActionButton(
        onPressed: _showQuickActionsSheet,
        child: const Icon(Icons.add),
      ),
    );
  }

  Color _getTypeColor(String type) {
    switch (type) {
      case 'workout_plan':
        return Colors.blue;
      case 'meal_plan':
        return Colors.green;
      case 'progress_analysis':
        return Colors.orange;
      default:
        return Colors.purple;
    }
  }

  IconData _getTypeIcon(String type) {
    switch (type) {
      case 'workout_plan':
        return Icons.fitness_center;
      case 'meal_plan':
        return Icons.restaurant;
      case 'progress_analysis':
        return Icons.analytics;
      default:
        return Icons.chat;
    }
  }

  String _getTypeLabel(String type) {
    switch (type) {
      case 'workout_plan':
        return 'Workout Plan';
      case 'meal_plan':
        return 'Meal Plan';
      case 'progress_analysis':
        return 'Progress Analysis';
      default:
        return 'General Chat';
    }
  }

  String _formatDate(DateTime date) {
    final now = DateTime.now();
    final diff = now.difference(date);

    if (diff.inDays == 0) {
      return 'Today';
    } else if (diff.inDays == 1) {
      return 'Yesterday';
    } else if (diff.inDays < 7) {
      return '${diff.inDays}d ago';
    } else {
      return '${date.month}/${date.day}';
    }
  }
}
