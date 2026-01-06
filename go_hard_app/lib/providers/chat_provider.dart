import 'dart:async';
import 'package:flutter/foundation.dart';
import '../data/models/chat_conversation.dart';
import '../data/models/chat_message.dart';
import '../data/repositories/chat_repository.dart';
import '../core/services/connectivity_service.dart';

/// Provider for AI chat management
class ChatProvider extends ChangeNotifier {
  final ChatRepository _chatRepository;
  final ConnectivityService _connectivity;

  List<ChatConversation> _conversations = [];
  ChatConversation? _currentConversation;
  bool _isLoading = false;
  bool _isSending = false;
  String? _errorMessage;
  StreamSubscription<bool>? _connectivitySubscription;

  ChatProvider(this._chatRepository, this._connectivity) {
    // Don't auto-load conversations here - they'll be loaded after login
    // This prevents trying to load conversations before user is authenticated

    // Listen for connectivity changes and refresh when going online
    _connectivitySubscription = _connectivity.connectivityStream.listen((
      isOnline,
    ) {
      if (isOnline) {
        debugPrint('📡 Connection restored - refreshing conversations');
        loadConversations(showLoading: false);
      }
    });
  }

  // Getters
  List<ChatConversation> get conversations => _conversations;
  ChatConversation? get currentConversation => _currentConversation;
  bool get isLoading => _isLoading;
  bool get isSending => _isSending;
  String? get errorMessage => _errorMessage;
  bool get isOffline => !_connectivity.isOnline;

  /// Load all conversations for current user
  Future<void> loadConversations({bool showLoading = true}) async {
    if (_isLoading) return;

    if (showLoading) {
      _isLoading = true;
      _errorMessage = null;
      notifyListeners();
    }

    try {
      final conversationList = await _chatRepository.getConversations();
      _conversations = conversationList;
    } catch (e) {
      _errorMessage =
          'Failed to load conversations: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Load conversations error: $e');
    } finally {
      if (showLoading) {
        _isLoading = false;
      }
      notifyListeners();
    }
  }

  /// Load a specific conversation with all messages
  Future<void> loadConversation(int conversationId) async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final conversation = await _chatRepository.getConversation(
        conversationId,
      );
      _currentConversation = conversation;
    } catch (e) {
      _errorMessage =
          'Failed to load conversation: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Load conversation error: $e');
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  /// Create a new conversation
  Future<ChatConversation?> createConversation({
    required String title,
    required String type,
  }) async {
    if (isOffline) {
      _errorMessage = 'Cannot create conversations offline';
      notifyListeners();
      return null;
    }

    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final conversation = await _chatRepository.createConversation(
        title: title,
        type: type,
      );

      if (conversation != null) {
        _conversations.insert(0, conversation);
        _currentConversation = conversation;
      }

      return conversation;
    } catch (e) {
      _errorMessage =
          'Failed to create conversation: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Create conversation error: $e');
      return null;
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  /// Send a message and get AI response
  Future<bool> sendMessage(String message) async {
    if (isOffline) {
      _errorMessage = 'Cannot send messages offline - AI requires connection';
      notifyListeners();
      return false;
    }

    if (_currentConversation == null) {
      _errorMessage = 'No active conversation';
      notifyListeners();
      return false;
    }

    _isSending = true;
    _errorMessage = null;
    notifyListeners();

    try {
      // Add user message optimistically to UI
      final userMessage = ChatMessage(
        id: 0, // Temporary ID
        conversationId: _currentConversation!.id,
        role: 'user',
        content: message,
        createdAt: DateTime.now().toUtc(),
      );

      _currentConversation = _currentConversation!.copyWith(
        messages: [..._currentConversation!.messages, userMessage],
      );
      notifyListeners();

      // Send to server and get AI response
      final aiResponse = await _chatRepository.sendMessage(
        conversationId: _currentConversation!.id,
        message: message,
      );

      if (aiResponse != null) {
        // Update conversation with AI response
        _currentConversation = _currentConversation!.copyWith(
          messages: [..._currentConversation!.messages, aiResponse],
          lastMessageAt: aiResponse.createdAt,
        );

        // Update conversation in list
        final index = _conversations.indexWhere(
          (c) => c.id == _currentConversation!.id,
        );
        if (index != -1) {
          _conversations[index] = _currentConversation!;
        }

        return true;
      } else {
        // Remove optimistic user message on failure
        _currentConversation = _currentConversation!.copyWith(
          messages:
              _currentConversation!.messages.where((m) => m.id != 0).toList(),
        );
        _errorMessage = 'Failed to get AI response';
        return false;
      }
    } catch (e) {
      // Remove optimistic user message on error
      if (_currentConversation != null) {
        _currentConversation = _currentConversation!.copyWith(
          messages:
              _currentConversation!.messages.where((m) => m.id != 0).toList(),
        );
      }

      _errorMessage =
          'Failed to send message: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Send message error: $e');
      return false;
    } finally {
      _isSending = false;
      notifyListeners();
    }
  }

  /// Delete a conversation
  Future<bool> deleteConversation(int conversationId) async {
    if (isOffline) {
      _errorMessage = 'Cannot delete conversations offline';
      notifyListeners();
      return false;
    }

    try {
      final success = await _chatRepository.deleteConversation(conversationId);

      if (success) {
        _conversations.removeWhere((c) => c.id == conversationId);

        // Clear current conversation if it was deleted
        if (_currentConversation?.id == conversationId) {
          _currentConversation = null;
        }

        notifyListeners();
        return true;
      } else {
        _errorMessage = 'Failed to delete conversation';
        notifyListeners();
        return false;
      }
    } catch (e) {
      _errorMessage =
          'Failed to delete conversation: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Delete conversation error: $e');
      notifyListeners();
      return false;
    }
  }

  /// Delete all conversations
  Future<bool> deleteAllConversations() async {
    if (isOffline) {
      _errorMessage = 'Cannot delete conversations offline';
      notifyListeners();
      return false;
    }

    if (_conversations.isEmpty) {
      return true;
    }

    try {
      // Delete all conversations one by one
      int successCount = 0;
      int failCount = 0;

      for (final conversation in List.from(_conversations)) {
        final success = await _chatRepository.deleteConversation(
          conversation.id,
        );
        if (success) {
          successCount++;
        } else {
          failCount++;
        }
      }

      // Clear local state
      _conversations.clear();
      _currentConversation = null;
      notifyListeners();

      if (failCount > 0) {
        _errorMessage =
            'Deleted $successCount conversations, $failCount failed';
        debugPrint('⚠️ Some conversations failed to delete');
      } else {
        debugPrint('✅ Deleted all $successCount conversations');
      }

      return failCount == 0;
    } catch (e) {
      _errorMessage =
          'Failed to delete conversations: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Delete all conversations error: $e');
      notifyListeners();
      return false;
    }
  }

  /// Generate workout plan (creates conversation with AI plan)
  Future<ChatConversation?> generateWorkoutPlan({
    required String goal,
    required String experienceLevel,
    required int daysPerWeek,
    required String equipment,
    String? limitations,
  }) async {
    if (isOffline) {
      _errorMessage = 'Cannot generate workout plan offline';
      notifyListeners();
      return null;
    }

    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final conversation = await _chatRepository.generateWorkoutPlan(
        goal: goal,
        experienceLevel: experienceLevel,
        daysPerWeek: daysPerWeek,
        equipment: equipment,
        limitations: limitations,
      );

      if (conversation != null) {
        _conversations.insert(0, conversation);
        _currentConversation = conversation;
      }

      return conversation;
    } catch (e) {
      _errorMessage =
          'Failed to generate workout plan: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Generate workout plan error: $e');
      return null;
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  /// Generate meal plan (creates conversation with AI meal plan)
  Future<ChatConversation?> generateMealPlan({
    required String dietaryGoal,
    int? targetCalories,
    String? macros,
    String? restrictions,
    String? preferences,
  }) async {
    if (isOffline) {
      _errorMessage = 'Cannot generate meal plan offline';
      notifyListeners();
      return null;
    }

    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final conversation = await _chatRepository.generateMealPlan(
        dietaryGoal: dietaryGoal,
        targetCalories: targetCalories,
        macros: macros,
        restrictions: restrictions,
        preferences: preferences,
      );

      if (conversation != null) {
        _conversations.insert(0, conversation);
        _currentConversation = conversation;
      }

      return conversation;
    } catch (e) {
      _errorMessage =
          'Failed to generate meal plan: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Generate meal plan error: $e');
      return null;
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  /// Analyze user's progress (creates conversation with AI analysis)
  Future<ChatConversation?> analyzeProgress({
    DateTime? startDate,
    DateTime? endDate,
    String? focusArea,
  }) async {
    if (isOffline) {
      _errorMessage = 'Cannot analyze progress offline';
      notifyListeners();
      return null;
    }

    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final conversation = await _chatRepository.analyzeProgress(
        startDate: startDate,
        endDate: endDate,
        focusArea: focusArea,
      );

      if (conversation != null) {
        _conversations.insert(0, conversation);
        _currentConversation = conversation;
      }

      return conversation;
    } catch (e) {
      _errorMessage =
          'Failed to analyze progress: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Analyze progress error: $e');
      return null;
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  /// Refresh conversations (pull-to-refresh)
  Future<void> refresh() async {
    await loadConversations(showLoading: false);
  }

  /// Clear current conversation
  void clearCurrentConversation() {
    _currentConversation = null;
    notifyListeners();
  }

  /// Clear error message
  void clearError() {
    _errorMessage = null;
    notifyListeners();
  }

  /// Preview workout sessions from the current workout plan conversation
  Future<Map<String, dynamic>?> previewSessionsFromPlan() async {
    if (isOffline) {
      _errorMessage = 'Cannot preview sessions offline';
      notifyListeners();
      return null;
    }

    if (_currentConversation == null) {
      _errorMessage = 'No active conversation';
      notifyListeners();
      return null;
    }

    if (_currentConversation!.type != 'workout_plan') {
      _errorMessage = 'This conversation is not a workout plan';
      notifyListeners();
      return null;
    }

    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final result = await _chatRepository.previewSessionsFromPlan(
        conversationId: _currentConversation!.id,
      );

      return result;
    } catch (e) {
      _errorMessage =
          'Failed to preview sessions: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Preview sessions error: $e');
      return null;
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  /// Create workout sessions from the current workout plan conversation
  Future<Map<String, dynamic>?> createSessionsFromPlan({
    DateTime? startDate,
  }) async {
    if (isOffline) {
      _errorMessage = 'Cannot create sessions offline';
      notifyListeners();
      return null;
    }

    if (_currentConversation == null) {
      _errorMessage = 'No active conversation';
      notifyListeners();
      return null;
    }

    if (_currentConversation!.type != 'workout_plan') {
      _errorMessage = 'This conversation is not a workout plan';
      notifyListeners();
      return null;
    }

    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final result = await _chatRepository.createSessionsFromPlan(
        conversationId: _currentConversation!.id,
        startDate: startDate,
      );

      return result;
    } catch (e) {
      _errorMessage =
          'Failed to create sessions: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Create sessions error: $e');
      return null;
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  /// Clear all chat data (called on logout)
  void clear() {
    _conversations = [];
    _currentConversation = null;
    _errorMessage = null;
    _isLoading = false;
    _isSending = false;
    notifyListeners();
    debugPrint('🧹 ChatProvider cleared');
  }

  @override
  void dispose() {
    _connectivitySubscription?.cancel();
    super.dispose();
  }
}
