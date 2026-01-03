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

  /// Load session by ID and calculate elapsed time
  Future<void> loadSession(int sessionId, {bool showLoading = true}) async {
    if (showLoading) {
      _isLoading = true;
      _errorMessage = null;
      notifyListeners();
    }

    try {
      _currentSession = await _sessionRepository.getSession(sessionId);

      // Calculate elapsed time if session has started
      // (startedAt and pausedAt are already in UTC from Session.fromJson)
      if (_currentSession?.startedAt != null) {
        final Duration calculated;

        if (_currentSession?.pausedAt != null) {
          // Timer is paused - elapsed time is when it was paused
          calculated = _currentSession!.pausedAt!.difference(
            _currentSession!.startedAt!,
          );
          _stopTimer(); // Ensure timer is stopped
        } else {
          // Timer is running - calculate from current time
          calculated = DateTime.now().toUtc().difference(
            _currentSession!.startedAt!,
          );
          // Auto-start timer since it should be running in background
          if (_currentSession?.status == 'in_progress') {
            _startTimer();
          }
        }

        // Ensure elapsed time is never negative (due to network latency)
        _elapsedTime = calculated.isNegative ? Duration.zero : calculated;
      } else {
        // Session hasn't started yet (still draft), reset timer
        _elapsedTime = Duration.zero;
        _stopTimer();
      }
    } catch (e) {
      _errorMessage =
          'Failed to load session: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Load session error: $e');
    } finally {
      if (showLoading) {
        _isLoading = false;
      }
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
      // Update DB and get session with correct timestamps
      final updatedSession = await _sessionRepository.updateSessionStatus(
        _currentSession!.id,
        'in_progress',
      );

      // Use the session from DB to ensure timestamps match
      _currentSession = updatedSession;
      _elapsedTime = Duration.zero;
      _startTimer();
      debugPrint('🏋️ Workout started with DB timestamps');
      notifyListeners();
    } catch (e) {
      _errorMessage =
          'Failed to start workout: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Start workout error: $e');
      notifyListeners();
    }
  }

  /// Pause the timer (keeps session in_progress but stops timer)
  Future<void> pauseTimer() async {
    if (_currentSession == null) return;

    // If session hasn't started yet, start it first
    if (_currentSession!.startedAt == null) {
      await startWorkout();
      return;
    }

    // Update UI IMMEDIATELY - don't wait for anything
    _stopTimer();
    _currentSession = _currentSession!.copyWith(
      pausedAt: DateTime.now().toUtc(),
    );
    notifyListeners();
    debugPrint('⏸️ Timer paused (UI updated)');

    // Then save to DB in background (don't block UI)
    _sessionRepository.pauseSession(_currentSession!.id).catchError((e) {
      _errorMessage =
          'Failed to pause: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Pause error: $e');
      notifyListeners();
    });
  }

  /// Resume the timer (continues from current elapsed time)
  Future<void> resumeTimer() async {
    if (_currentSession == null) return;

    // If session hasn't started yet, start it instead of resuming
    if (_currentSession!.startedAt == null) {
      await startWorkout();
      return;
    }

    // Update UI IMMEDIATELY - don't wait for anything
    final now = DateTime.now().toUtc();
    final pauseDuration =
        _currentSession!.pausedAt != null
            ? now.difference(_currentSession!.pausedAt!)
            : Duration.zero;
    final newStartedAt = _currentSession!.startedAt!.add(pauseDuration);

    _currentSession = _currentSession!.copyWith(
      startedAt: newStartedAt,
      pausedAt: null, // Clear pausedAt
    );

    // Resume timer
    _startTimer();
    notifyListeners();
    debugPrint('▶️ Timer resumed (UI updated)');

    // Then save to DB in background (don't block UI)
    _sessionRepository.resumeSession(_currentSession!.id).catchError((e) {
      _errorMessage =
          'Failed to resume: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Resume error: $e');
      notifyListeners();
    });
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
      _errorMessage =
          'Failed to finish workout: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Finish workout error: $e');
      notifyListeners();
      return false;
    }
  }

  /// Add exercise to current session
  Future<void> addExercise(int exerciseTemplateId) async {
    if (_currentSession == null) return;

    try {
      // Add exercise and get the new exercise object
      final newExercise = await _sessionRepository.addExerciseToSession(
        _currentSession!.id,
        exerciseTemplateId,
      );

      // Add exercise to current session's list (don't reload entire session)
      final updatedExercises = [..._currentSession!.exercises, newExercise];
      _currentSession = _currentSession!.copyWith(exercises: updatedExercises);

      debugPrint('✅ Exercise added to session (timer preserved)');
      notifyListeners();
    } catch (e) {
      _errorMessage =
          'Failed to add exercise: ${e.toString().replaceAll('Exception: ', '')}';
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
