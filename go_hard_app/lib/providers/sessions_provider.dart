import 'dart:async';
import 'package:flutter/foundation.dart';
import '../data/models/session.dart';
import '../data/repositories/session_repository.dart';
import '../data/services/auth_service.dart';
import '../core/services/connectivity_service.dart';

/// Provider for sessions (workouts) management
/// Replaces SessionsViewModel from MAUI app
class SessionsProvider extends ChangeNotifier {
  final SessionRepository _sessionRepository;
  final AuthService _authService;
  final ConnectivityService _connectivity;

  List<Session> _sessions = [];
  bool _isLoading = false;
  String? _errorMessage;
  StreamSubscription<bool>? _connectivitySubscription;

  SessionsProvider(
    this._sessionRepository,
    this._authService,
    this._connectivity,
  ) {
    // Don't auto-load sessions here - they'll be loaded after login
    // This prevents trying to load sessions before user is authenticated

    // Listen for connectivity changes and refresh when going online
    _connectivitySubscription = _connectivity.connectivityStream.listen((
      isOnline,
    ) {
      if (isOnline) {
        debugPrint('📡 Connection restored - refreshing sessions');
        loadSessions(showLoading: false); // Refresh without loading indicator
      }
    });
  }

  // Getters
  List<Session> get sessions => _sessions;
  bool get isLoading => _isLoading;
  String? get errorMessage => _errorMessage;

  /// Load all sessions for current user
  /// Set [waitForSync] to true to wait for server sync (useful after login when cache is empty)
  Future<void> loadSessions({
    bool showLoading = true,
    bool waitForSync = false,
  }) async {
    debugPrint(
      '🔄 SessionsProvider.loadSessions() called - waitForSync: $waitForSync, showLoading: $showLoading',
    );

    if (_isLoading) {
      debugPrint('⏭️  Already loading, skipping...');
      return;
    }

    if (showLoading) {
      _isLoading = true;
      _errorMessage = null;
      notifyListeners();
    }

    try {
      final sessionList = await _sessionRepository.getSessions(
        waitForSync: waitForSync,
      );
      // Sort by date descending (most recent first)
      _sessions = sessionList..sort((a, b) => b.date.compareTo(a.date));
      debugPrint('✅ Loaded ${_sessions.length} sessions into provider');
    } catch (e) {
      _errorMessage =
          'Failed to load sessions: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('❌ Load sessions error: $e');
    } finally {
      if (showLoading) {
        _isLoading = false;
      }
      debugPrint(
        '📢 Calling notifyListeners() with ${_sessions.length} sessions',
      );
      notifyListeners();
    }
  }

  /// Get session by ID
  Future<Session> getSessionById(int sessionId) async {
    return await _sessionRepository.getSession(sessionId);
  }

  /// Delete a session by ID
  Future<bool> deleteSession(int sessionId) async {
    try {
      final success = await _sessionRepository.deleteSession(sessionId);
      if (success) {
        _sessions.removeWhere((s) => s.id == sessionId);
        notifyListeners();
        return true;
      } else {
        _errorMessage = 'Failed to delete session';
        notifyListeners();
        return false;
      }
    } catch (e) {
      _errorMessage =
          'Failed to delete session: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Delete session error: $e');
      notifyListeners();
      return false;
    }
  }

  /// Start a new workout session
  Future<Session?> startNewWorkout({String? name}) async {
    try {
      // Get current user ID
      final userId = await _authService.getUserId();
      if (userId == null) {
        _errorMessage = 'User not authenticated';
        notifyListeners();
        return null;
      }

      // Create a new draft session with today's date (no time conversion)
      final now = DateTime.now();
      final newSession = Session(
        id: 0, // Will be assigned by server
        userId: userId,
        date: DateTime(now.year, now.month, now.day),
        type: 'Workout',
        status: 'draft',
        notes: '',
        name: name, // Set the workout name
      );

      final createdSession = await _sessionRepository.createSession(newSession);

      // Add to local list
      _sessions.insert(0, createdSession);
      notifyListeners();

      return createdSession;
    } catch (e) {
      _errorMessage =
          'Failed to start workout: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Start workout error: $e');
      notifyListeners();
      return null;
    }
  }

  /// Start a planned workout (change status from 'planned' to 'in_progress')
  Future<bool> startPlannedWorkout(int sessionId) async {
    try {
      // Update session status via repository
      final updatedSession = await _sessionRepository.updateSessionStatus(
        sessionId,
        'in_progress',
      );

      // Update local session in the list
      final index = _sessions.indexWhere((s) => s.id == sessionId);
      if (index != -1) {
        _sessions[index] = updatedSession;
        notifyListeners();
      }

      return true;
    } catch (e) {
      _errorMessage =
          'Failed to start planned workout: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Start planned workout error: $e');
      notifyListeners();
      return false;
    }
  }

  /// Refresh sessions (pull-to-refresh)
  /// Don't show loading indicator for smooth UX
  Future<void> refresh() async {
    await loadSessions(showLoading: false);
  }

  /// Clear error message
  void clearError() {
    _errorMessage = null;
    notifyListeners();
  }

  /// Clear all sessions data (called on logout)
  void clear() {
    _sessions = [];
    _errorMessage = null;
    _isLoading = false;
    notifyListeners();
    debugPrint('🧹 SessionsProvider cleared');
  }

  @override
  void dispose() {
    _connectivitySubscription?.cancel();
    super.dispose();
  }
}
