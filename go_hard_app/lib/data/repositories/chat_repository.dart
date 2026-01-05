import 'package:flutter/foundation.dart';
import 'package:isar/isar.dart';
import '../../core/constants/api_config.dart';
import '../../core/services/connectivity_service.dart';
import '../models/chat_conversation.dart';
import '../models/chat_message.dart';
import '../services/api_service.dart';
import '../services/auth_service.dart';
import '../local/services/local_database_service.dart';
import '../local/models/local_chat_conversation.dart';
import '../local/models/local_chat_message.dart';

/// Repository for chat operations with offline support
/// Note: Sending messages requires online connection (AI responses)
class ChatRepository {
  final ApiService _apiService;
  final LocalDatabaseService _localDb;
  final ConnectivityService _connectivity;
  final AuthService _authService;

  ChatRepository(
    this._apiService,
    this._localDb,
    this._connectivity,
    this._authService,
  );

  /// Get all conversations for the current user
  /// Offline-first: returns local cache immediately, syncs with server in background
  Future<List<ChatConversation>> getConversations() async {
    final Isar db = _localDb.database;

    // ALWAYS load from cache first for instant response
    final cachedConversations = await _getLocalConversations(db);

    // Then sync with server in background if online (don't block)
    if (_connectivity.isOnline) {
      _syncConversationsFromServer(db)
          .then((_) {
            debugPrint('✅ Background sync: Conversations synced from server');
          })
          .catchError((e) {
            debugPrint('⚠️ Background sync failed: $e');
          });
    }

    return cachedConversations;
  }

  /// Get local conversations from cache
  Future<List<ChatConversation>> _getLocalConversations(Isar db) async {
    final localConvos =
        await db.localChatConversations
            .filter()
            .isArchivedEqualTo(false)
            .sortByLastMessageAtDesc()
            .findAll();

    return localConvos
        .map(
          (local) => ChatConversation(
            id: local.serverId ?? 0,
            userId: local.userId,
            title: local.title,
            type: local.type,
            createdAt: local.createdAt,
            lastMessageAt: local.lastMessageAt,
            isArchived: local.isArchived,
            messageCount: 0, // Will be loaded when conversation is opened
          ),
        )
        .toList();
  }

  /// Background sync: Fetch conversations from server and update cache
  Future<void> _syncConversationsFromServer(Isar db) async {
    try {
      final currentUserId = await _authService.getUserId();
      if (currentUserId == null) {
        debugPrint('⚠️ No authenticated user, skipping conversation sync');
        return;
      }

      // Fetch from API
      final data = await _apiService.get<List<dynamic>>(
        ApiConfig.chatConversations,
      );
      final apiConversations =
          data
              .map(
                (json) =>
                    ChatConversation.fromJson(json as Map<String, dynamic>),
              )
              .toList();

      // Update local cache
      await db.writeTxn(() async {
        for (final apiConvo in apiConversations) {
          // Only cache conversations belonging to current user
          if (apiConvo.userId != currentUserId) {
            continue;
          }

          // Check if conversation already exists locally
          final existingLocal =
              await db.localChatConversations
                  .filter()
                  .serverIdEqualTo(apiConvo.id)
                  .findFirst();

          if (existingLocal != null &&
              existingLocal.syncStatus == 'pending_delete') {
            continue; // Skip conversations marked for deletion
          }

          if (existingLocal != null) {
            // Update existing local conversation
            existingLocal.title = apiConvo.title;
            existingLocal.type = apiConvo.type;
            existingLocal.lastMessageAt = apiConvo.lastMessageAt;
            existingLocal.isArchived = apiConvo.isArchived;
            existingLocal.isSynced = true;
            existingLocal.syncStatus = 'synced';
            existingLocal.lastModifiedServer = DateTime.now().toUtc();

            await db.localChatConversations.put(existingLocal);
          } else {
            // Insert new conversation
            final newLocal = LocalChatConversation(
              serverId: apiConvo.id,
              userId: apiConvo.userId,
              title: apiConvo.title,
              type: apiConvo.type,
              createdAt: apiConvo.createdAt,
              lastMessageAt: apiConvo.lastMessageAt,
              isArchived: apiConvo.isArchived,
              isSynced: true,
              syncStatus: 'synced',
              lastModifiedLocal: DateTime.now().toUtc(),
              lastModifiedServer: DateTime.now().toUtc(),
            );

            await db.localChatConversations.put(newLocal);
          }
        }
      });
    } catch (e) {
      debugPrint('❌ Error syncing conversations: $e');
      rethrow;
    }
  }

  /// Get a single conversation with all messages
  /// Requires online connection to fetch messages
  Future<ChatConversation?> getConversation(int conversationId) async {
    if (!_connectivity.isOnline) {
      // Try to load from cache if offline
      return await _getLocalConversation(conversationId);
    }

    try {
      final data = await _apiService.get<Map<String, dynamic>>(
        ApiConfig.chatConversationById(conversationId),
      );
      final conversation = ChatConversation.fromJson(data);

      // Cache the conversation and messages
      await _cacheConversation(conversation);

      return conversation;
    } catch (e) {
      debugPrint('❌ Error fetching conversation: $e');
      // Fallback to cache if API fails
      return await _getLocalConversation(conversationId);
    }
  }

  /// Get conversation from local cache
  Future<ChatConversation?> _getLocalConversation(int conversationId) async {
    final db = _localDb.database;

    final localConvo =
        await db.localChatConversations
            .filter()
            .serverIdEqualTo(conversationId)
            .findFirst();

    if (localConvo == null) return null;

    // Load messages for this conversation
    final localMessages =
        await db.localChatMessages
            .filter()
            .conversationServerIdEqualTo(conversationId)
            .sortByCreatedAt()
            .findAll();

    final messages =
        localMessages
            .map(
              (local) => ChatMessage(
                id: local.serverId ?? 0,
                conversationId: conversationId,
                role: local.role,
                content: local.content,
                createdAt: local.createdAt,
                inputTokens: local.inputTokens,
                outputTokens: local.outputTokens,
                model: local.model,
              ),
            )
            .toList();

    return ChatConversation(
      id: conversationId,
      userId: localConvo.userId,
      title: localConvo.title,
      type: localConvo.type,
      createdAt: localConvo.createdAt,
      lastMessageAt: localConvo.lastMessageAt,
      isArchived: localConvo.isArchived,
      messages: messages,
    );
  }

  /// Cache a conversation and its messages locally
  Future<void> _cacheConversation(ChatConversation conversation) async {
    final db = _localDb.database;
    final currentUserId = await _authService.getUserId();
    if (currentUserId == null) return;

    await db.writeTxn(() async {
      // Cache conversation
      final existingLocal =
          await db.localChatConversations
              .filter()
              .serverIdEqualTo(conversation.id)
              .findFirst();

      if (existingLocal != null) {
        existingLocal.title = conversation.title;
        existingLocal.type = conversation.type;
        existingLocal.lastMessageAt = conversation.lastMessageAt;
        existingLocal.isArchived = conversation.isArchived;
        existingLocal.isSynced = true;
        existingLocal.syncStatus = 'synced';

        await db.localChatConversations.put(existingLocal);

        // Cache messages
        for (final message in conversation.messages) {
          final existingMessage =
              await db.localChatMessages
                  .filter()
                  .serverIdEqualTo(message.id)
                  .findFirst();

          if (existingMessage == null) {
            final newMessage = LocalChatMessage(
              serverId: message.id,
              conversationLocalId: existingLocal.localId,
              conversationServerId: conversation.id,
              role: message.role,
              content: message.content,
              createdAt: message.createdAt,
              inputTokens: message.inputTokens,
              outputTokens: message.outputTokens,
              model: message.model,
              isSynced: true,
              syncStatus: 'synced',
              lastModifiedLocal: DateTime.now().toUtc(),
            );

            await db.localChatMessages.put(newMessage);
          }
        }
      } else {
        // Create new local conversation
        final newLocal = LocalChatConversation(
          serverId: conversation.id,
          userId: conversation.userId,
          title: conversation.title,
          type: conversation.type,
          createdAt: conversation.createdAt,
          lastMessageAt: conversation.lastMessageAt,
          isArchived: conversation.isArchived,
          isSynced: true,
          syncStatus: 'synced',
          lastModifiedLocal: DateTime.now().toUtc(),
        );

        final localId = await db.localChatConversations.put(newLocal);

        // Cache messages
        for (final message in conversation.messages) {
          final newMessage = LocalChatMessage(
            serverId: message.id,
            conversationLocalId: localId,
            conversationServerId: conversation.id,
            role: message.role,
            content: message.content,
            createdAt: message.createdAt,
            inputTokens: message.inputTokens,
            outputTokens: message.outputTokens,
            model: message.model,
            isSynced: true,
            syncStatus: 'synced',
            lastModifiedLocal: DateTime.now().toUtc(),
          );

          await db.localChatMessages.put(newMessage);
        }
      }
    });
  }

  /// Create a new conversation
  /// Requires online connection
  Future<ChatConversation?> createConversation({
    required String title,
    required String type,
  }) async {
    if (!_connectivity.isOnline) {
      throw Exception('Cannot create conversations offline');
    }

    try {
      final response = await _apiService.post<Map<String, dynamic>>(
        ApiConfig.chatConversations,
        data: {'title': title, 'type': type},
      );

      final conversation = ChatConversation.fromJson(response);

      // Cache locally
      await _cacheConversation(conversation);

      return conversation;
    } catch (e) {
      debugPrint('❌ Error creating conversation: $e');
      rethrow;
    }
  }

  /// Send a message and get AI response
  /// Requires online connection
  Future<ChatMessage?> sendMessage({
    required int conversationId,
    required String message,
  }) async {
    if (!_connectivity.isOnline) {
      throw Exception('Cannot send messages offline - AI requires connection');
    }

    try {
      final response = await _apiService.post<Map<String, dynamic>>(
        ApiConfig.chatMessages(conversationId),
        data: {'message': message, 'stream': false},
      );

      final aiMessage = ChatMessage.fromJson(response);

      // Cache the AI response locally
      await _cacheMessage(conversationId, message, aiMessage);

      return aiMessage;
    } catch (e) {
      debugPrint('❌ Error sending message: $e');
      rethrow;
    }
  }

  /// Cache user message and AI response locally
  Future<void> _cacheMessage(
    int conversationId,
    String userMessage,
    ChatMessage aiResponse,
  ) async {
    final db = _localDb.database;

    final localConvo =
        await db.localChatConversations
            .filter()
            .serverIdEqualTo(conversationId)
            .findFirst();

    if (localConvo == null) return;

    await db.writeTxn(() async {
      // Cache user message (it's not returned from API, create it locally)
      final userMsg = LocalChatMessage(
        conversationLocalId: localConvo.localId,
        conversationServerId: conversationId,
        role: 'user',
        content: userMessage,
        createdAt: DateTime.now().toUtc(),
        isSynced: true,
        syncStatus: 'synced',
        lastModifiedLocal: DateTime.now().toUtc(),
      );
      await db.localChatMessages.put(userMsg);

      // Cache AI response
      final aiMsg = LocalChatMessage(
        serverId: aiResponse.id,
        conversationLocalId: localConvo.localId,
        conversationServerId: conversationId,
        role: aiResponse.role,
        content: aiResponse.content,
        createdAt: aiResponse.createdAt,
        inputTokens: aiResponse.inputTokens,
        outputTokens: aiResponse.outputTokens,
        model: aiResponse.model,
        isSynced: true,
        syncStatus: 'synced',
        lastModifiedLocal: DateTime.now().toUtc(),
      );
      await db.localChatMessages.put(aiMsg);

      // Update conversation's lastMessageAt
      localConvo.lastMessageAt = aiResponse.createdAt;
      await db.localChatConversations.put(localConvo);
    });
  }

  /// Delete a conversation
  /// Requires online connection
  Future<bool> deleteConversation(int conversationId) async {
    if (!_connectivity.isOnline) {
      throw Exception('Cannot delete conversations offline');
    }

    try {
      await _apiService.delete(ApiConfig.chatConversationById(conversationId));

      // Remove from local cache
      final db = _localDb.database;
      await db.writeTxn(() async {
        final localConvo =
            await db.localChatConversations
                .filter()
                .serverIdEqualTo(conversationId)
                .findFirst();

        if (localConvo != null) {
          // Delete messages first
          final messages =
              await db.localChatMessages
                  .filter()
                  .conversationLocalIdEqualTo(localConvo.localId)
                  .findAll();

          for (final msg in messages) {
            await db.localChatMessages.delete(msg.localId);
          }

          // Delete conversation
          await db.localChatConversations.delete(localConvo.localId);
        }
      });

      return true;
    } catch (e) {
      debugPrint('❌ Error deleting conversation: $e');
      return false;
    }
  }

  /// Generate workout plan (creates conversation + first AI response)
  /// Requires online connection
  Future<ChatConversation?> generateWorkoutPlan({
    required String goal,
    required String experienceLevel,
    required int daysPerWeek,
    required String equipment,
    String? limitations,
  }) async {
    if (!_connectivity.isOnline) {
      throw Exception('Cannot generate workout plan offline');
    }

    try {
      final response = await _apiService.post<Map<String, dynamic>>(
        ApiConfig.chatWorkoutPlan,
        data: {
          'goal': goal,
          'experienceLevel': experienceLevel,
          'daysPerWeek': daysPerWeek,
          'equipment': equipment,
          'limitations': limitations ?? '',
        },
      );

      final conversation = ChatConversation.fromJson(response);

      // Cache locally
      await _cacheConversation(conversation);

      return conversation;
    } catch (e) {
      debugPrint('❌ Error generating workout plan: $e');
      rethrow;
    }
  }

  /// Generate meal plan (creates conversation + first AI response)
  /// Requires online connection
  Future<ChatConversation?> generateMealPlan({
    required String dietaryGoal,
    int? targetCalories,
    String? macros,
    String? restrictions,
    String? preferences,
  }) async {
    if (!_connectivity.isOnline) {
      throw Exception('Cannot generate meal plan offline');
    }

    try {
      final response = await _apiService.post<Map<String, dynamic>>(
        ApiConfig.chatMealPlan,
        data: {
          'dietaryGoal': dietaryGoal,
          'targetCalories': targetCalories,
          'macros': macros ?? '',
          'restrictions': restrictions ?? '',
          'preferences': preferences ?? '',
        },
      );

      final conversation = ChatConversation.fromJson(response);

      // Cache locally
      await _cacheConversation(conversation);

      return conversation;
    } catch (e) {
      debugPrint('❌ Error generating meal plan: $e');
      rethrow;
    }
  }

  /// Analyze user's progress (creates conversation + AI analysis)
  /// Requires online connection
  Future<ChatConversation?> analyzeProgress({
    DateTime? startDate,
    DateTime? endDate,
    String? focusArea,
  }) async {
    if (!_connectivity.isOnline) {
      throw Exception('Cannot analyze progress offline');
    }

    try {
      final data = <String, dynamic>{};
      if (startDate != null) data['startDate'] = startDate.toIso8601String();
      if (endDate != null) data['endDate'] = endDate.toIso8601String();
      if (focusArea != null) data['focusArea'] = focusArea;

      final response = await _apiService.post<Map<String, dynamic>>(
        ApiConfig.chatAnalyzeProgress,
        data: data,
      );

      final conversation = ChatConversation.fromJson(response);

      // Cache locally
      await _cacheConversation(conversation);

      return conversation;
    } catch (e) {
      debugPrint('❌ Error analyzing progress: $e');
      rethrow;
    }
  }

  /// Preview workout sessions from an AI-generated workout plan (without creating)
  /// Requires online connection
  Future<Map<String, dynamic>> previewSessionsFromPlan({
    required int conversationId,
  }) async {
    if (!_connectivity.isOnline) {
      throw Exception('Cannot preview sessions offline');
    }

    try {
      final response = await _apiService.get<Map<String, dynamic>>(
        ApiConfig.chatPreviewSessions(conversationId),
      );

      debugPrint(
        '✅ Previewed ${response['sessionsCount']} sessions from workout plan',
      );

      return response;
    } catch (e) {
      debugPrint('❌ Error previewing sessions: $e');
      rethrow;
    }
  }

  /// Create workout sessions from an AI-generated workout plan
  /// Requires online connection - cannot create sessions offline
  Future<Map<String, dynamic>> createSessionsFromPlan({
    required int conversationId,
    DateTime? startDate,
  }) async {
    if (!_connectivity.isOnline) {
      throw Exception('Cannot create sessions offline');
    }

    try {
      final data = <String, dynamic>{};
      if (startDate != null) {
        data['startDate'] = startDate.toIso8601String();
      }

      final response = await _apiService.post<Map<String, dynamic>>(
        ApiConfig.chatCreateSessions(conversationId),
        data: data,
      );

      debugPrint(
        '✅ Created ${response['sessions'].length} sessions from workout plan',
      );

      return response;
    } catch (e) {
      debugPrint('❌ Error creating sessions from plan: $e');
      rethrow;
    }
  }
}
