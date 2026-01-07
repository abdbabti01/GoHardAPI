import 'dart:async';
import 'package:flutter/foundation.dart';
import '../data/models/session.dart';
import '../data/models/exercise.dart';
import '../data/repositories/session_repository.dart';
import '../core/services/connectivity_service.dart';

/// Provider for active workout session with timer
/// Replaces ActiveWorkoutViewModel from MAUI app
class ActiveWorkoutProvider extends ChangeNotifier {
  final SessionRepository _sessionRepository;
  final ConnectivityService? _connectivity;

  Session? _currentSession;
  bool _isLoading = false;
  String? _errorMessage;

  // Timer state
  Timer? _timer;
  Duration _elapsedTime = Duration.zero;
  bool _isTimerRunning = false;

  // Connectivity subscription
  StreamSubscription<bool>? _connectivitySubscription;

  ActiveWorkoutProvider(this._sessionRepository, [this._connectivity]) {
    // Listen for connectivity changes and refresh session when going online
    _connectivitySubscription = _connectivity?.connectivityStream.listen((
      isOnline,
    ) {
      if (isOnline && _currentSession != null) {
        debugPrint(
          '📡 Connection restored - refreshing active workout (ID: ${_currentSession!.id})',
        );
        loadSession(_currentSession!.id, showLoading: false);
      }
    });
  }

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

      debugPrint('⏱️ TIMER DEBUG - loadSession called');
      debugPrint('  Session ID: ${_currentSession?.id}');
      debugPrint('  Status: ${_currentSession?.status}');
      debugPrint('  StartedAt: ${_currentSession?.startedAt}');
      debugPrint('  PausedAt: ${_currentSession?.pausedAt}');
      debugPrint('  Current time UTC: ${DateTime.now().toUtc()}');

      // Calculate elapsed time if session has started
      // (startedAt and pausedAt are already in UTC from Session.fromJson)
      if (_currentSession?.startedAt != null) {
        final Duration calculated;
        final bool shouldBeRunning;

        if (_currentSession?.pausedAt != null) {
          // Timer is paused - elapsed time is when it was paused
          calculated = _currentSession!.pausedAt!.difference(
            _currentSession!.startedAt!,
          );
          shouldBeRunning = false;
          debugPrint('  Timer PAUSED - calculated: ${calculated.inSeconds}s');
        } else {
          // Timer is running - calculate from current time
          calculated = DateTime.now().toUtc().difference(
            _currentSession!.startedAt!,
          );
          shouldBeRunning = _currentSession?.status == 'in_progress';
          debugPrint('  Timer RUNNING - calculated: ${calculated.inSeconds}s');
        }

        // CRITICAL: Always stop timer first to avoid race condition
        _stopTimer();

        // Set the correct elapsed time
        _elapsedTime = calculated.isNegative ? Duration.zero : calculated;
        debugPrint('  Set _elapsedTime to: ${_elapsedTime.inSeconds}s');

        // Then restart timer if it should be running
        if (shouldBeRunning) {
          _startTimer();
          debugPrint('  Timer restarted');
        }
      } else {
        // Session hasn't started yet (still draft), reset timer
        debugPrint('  StartedAt is NULL - resetting timer to zero');
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
    final nowUtc = DateTime.now().toUtc(); // CRITICAL: Use UTC
    _stopTimer();
    _currentSession = _currentSession!.copyWith(
      pausedAt: nowUtc, // Store as UTC
    );
    notifyListeners();
    debugPrint('⏸️ Timer paused (UI updated) - pausedAt UTC: $nowUtc');

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

  /// Update workout name
  Future<bool> updateWorkoutName(String name) async {
    if (_currentSession == null) return false;

    try {
      _currentSession = await _sessionRepository.updateSessionName(
        _currentSession!.id,
        name,
      );
      notifyListeners();
      return true;
    } catch (e) {
      _errorMessage =
          'Failed to update workout name: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Update workout name error: $e');
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

  /// Create a new workout session from AI-generated exercises
  /// Returns the session ID if successful, null otherwise
  Future<int?> createWorkoutFromAI({
    required String workoutName,
    required List<int> exerciseTemplateIds,
  }) async {
    try {
      _isLoading = true;
      _errorMessage = null;
      notifyListeners();

      // Create new session
      final newSession = Session(
        id: 0, // Will be assigned by repository
        userId: 0, // Will be set by repository
        date: DateTime.now(),
        duration: 0,
        name: workoutName,
        type: 'strength',
        status: 'draft',
        exercises: const [],
      );

      // Save session
      final createdSession = await _sessionRepository.createSession(newSession);
      _currentSession = createdSession;

      debugPrint('✅ Created AI workout session: ${createdSession.id}');

      // Add exercises to the session
      for (final templateId in exerciseTemplateIds) {
        try {
          final exercise = await _sessionRepository.addExerciseToSession(
            createdSession.id,
            templateId,
          );
          // Add to current session's list
          final updatedExercises = [..._currentSession!.exercises, exercise];
          _currentSession = _currentSession!.copyWith(
            exercises: updatedExercises,
          );
          debugPrint('  ✅ Added exercise: ${exercise.name}');
        } catch (e) {
          debugPrint('  ⚠️ Failed to add exercise template $templateId: $e');
          // Continue adding other exercises even if one fails
        }
      }

      notifyListeners();
      return createdSession.id;
    } catch (e) {
      _errorMessage =
          'Failed to create workout: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Create workout from AI error: $e');
      notifyListeners();
      return null;
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  /// Clear error message
  void clearError() {
    _errorMessage = null;
    notifyListeners();
  }

  /// Clear all workout data (called on logout)
  void clear() {
    _stopTimer();
    _currentSession = null;
    _errorMessage = null;
    _isLoading = false;
    _elapsedTime = Duration.zero;
    _isTimerRunning = false;
    notifyListeners();
    debugPrint('🧹 ActiveWorkoutProvider cleared');
  }

  @override
  void dispose() {
    _stopTimer();
    _connectivitySubscription?.cancel();
    super.dispose();
  }
}
