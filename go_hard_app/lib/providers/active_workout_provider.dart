import 'dart:async';
import 'package:flutter/foundation.dart';
import '../data/models/session.dart';
import '../data/models/exercise.dart';
import '../data/repositories/session_repository.dart';

/// Provider for active workout session with timer
/// Replaces ActiveWorkoutViewModel from MAUI app
class ActiveWorkoutProvider extends ChangeNotifier {
  final SessionRepository _sessionRepository;

  Session? _currentSession;
  bool _isLoading = false;
  String? _errorMessage;

  // Timer state
  Timer? _timer;
  Duration _elapsedTime = Duration.zero;
  bool _isTimerRunning = false;

  ActiveWorkoutProvider(this._sessionRepository);

  // Getters
  Session? get currentSession => _currentSession;
  bool get isLoading => _isLoading;
  String? get errorMessage => _errorMessage;
  Duration get elapsedTime => _elapsedTime;
  bool get isTimerRunning => _isTimerRunning;
  List<Exercise> get exercises => _currentSession?.exercises ?? [];

  /// Load session by ID and start timer if in progress
  Future<void> loadSession(int sessionId) async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      _currentSession = await _sessionRepository.getSession(sessionId);

      // Calculate elapsed time if session has started
      if (_currentSession?.startedAt != null) {
        _elapsedTime = DateTime.now().difference(_currentSession!.startedAt!);
        _startTimer();
      }
    } catch (e) {
      _errorMessage = 'Failed to load session: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Load session error: $e');
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  /// Start the workout timer
  void _startTimer() {
    if (_isTimerRunning) return;

    _isTimerRunning = true;
    _timer = Timer.periodic(const Duration(seconds: 1), (timer) {
      _elapsedTime += const Duration(seconds: 1);
      notifyListeners();
    });
  }

  /// Stop the workout timer
  void _stopTimer() {
    _timer?.cancel();
    _timer = null;
    _isTimerRunning = false;
  }

  /// Start workout (update status to in_progress)
  Future<void> startWorkout() async {
    if (_currentSession == null) return;

    try {
      _currentSession = await _sessionRepository.updateSessionStatus(
        _currentSession!.id,
        'in_progress',
      );
      _startTimer();
      notifyListeners();
    } catch (e) {
      _errorMessage = 'Failed to start workout: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Start workout error: $e');
      notifyListeners();
    }
  }

  /// Finish workout (update status to completed)
  Future<bool> finishWorkout() async {
    if (_currentSession == null) return false;

    try {
      _stopTimer();
      _currentSession = await _sessionRepository.updateSessionStatus(
        _currentSession!.id,
        'completed',
      );
      notifyListeners();
      return true;
    } catch (e) {
      _errorMessage = 'Failed to finish workout: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Finish workout error: $e');
      notifyListeners();
      return false;
    }
  }

  /// Add exercise to current session
  Future<void> addExercise(Exercise exercise) async {
    if (_currentSession == null) return;

    try {
      await _sessionRepository.addExerciseToSession(
        _currentSession!.id,
        exercise,
      );

      // Reload session to get updated exercises
      await loadSession(_currentSession!.id);
    } catch (e) {
      _errorMessage = 'Failed to add exercise: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Add exercise error: $e');
      notifyListeners();
    }
  }

  /// Clear error message
  void clearError() {
    _errorMessage = null;
    notifyListeners();
  }

  @override
  void dispose() {
    _stopTimer();
    super.dispose();
  }
}
