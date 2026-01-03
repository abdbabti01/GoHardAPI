import 'package:flutter/foundation.dart';
import '../data/models/session.dart';
import '../data/repositories/session_repository.dart';
import '../data/services/auth_service.dart';

/// Provider for sessions (workouts) management
/// Replaces SessionsViewModel from MAUI app
class SessionsProvider extends ChangeNotifier {
  final SessionRepository _sessionRepository;
  final AuthService _authService;

  List<Session> _sessions = [];
  bool _isLoading = false;
  String? _errorMessage;

  SessionsProvider(this._sessionRepository, this._authService) {
    // Auto-load sessions on initialization to cache them for offline use
    loadSessions();
  }

  // Getters
  List<Session> get sessions => _sessions;
  bool get isLoading => _isLoading;
  String? get errorMessage => _errorMessage;

  /// Load all sessions for current user
  Future<void> loadSessions({bool showLoading = true}) async {
    if (_isLoading) return;

    if (showLoading) {
      _isLoading = true;
      _errorMessage = null;
      notifyListeners();
    }

    try {
      final sessionList = await _sessionRepository.getSessions();
      // Sort by date descending (most recent first)
      _sessions = sessionList..sort((a, b) => b.date.compareTo(a.date));
    } catch (e) {
      _errorMessage =
          'Failed to load sessions: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Load sessions error: $e');
    } finally {
      if (showLoading) {
        _isLoading = false;
      }
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
  Future<Session?> startNewWorkout() async {
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
}
