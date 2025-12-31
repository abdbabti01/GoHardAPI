import '../../core/constants/api_config.dart';
import '../models/session.dart';
import '../models/exercise.dart';
import '../services/api_service.dart';

/// Repository for session (workout) operations
class SessionRepository {
  final ApiService _apiService;

  SessionRepository(this._apiService);

  /// Get all sessions for the current user
  Future<List<Session>> getSessions() async {
    final data = await _apiService.get<List<dynamic>>(ApiConfig.sessions);
    return data.map((json) => Session.fromJson(json as Map<String, dynamic>)).toList();
  }

  /// Get session by ID
  Future<Session> getSession(int id) async {
    final data = await _apiService.get<Map<String, dynamic>>(
      ApiConfig.sessionById(id),
    );
    return Session.fromJson(data);
  }

  /// Create new session
  Future<Session> createSession(Session session) async {
    final data = await _apiService.post<Map<String, dynamic>>(
      ApiConfig.sessions,
      data: session.toJson(),
    );
    return Session.fromJson(data);
  }

  /// Update session status
  Future<Session> updateSessionStatus(int id, String status) async {
    final data = await _apiService.patch<Map<String, dynamic>>(
      ApiConfig.sessionStatus(id),
      data: {'status': status},
    );
    return Session.fromJson(data);
  }

  /// Delete session
  Future<bool> deleteSession(int id) async {
    return await _apiService.delete(ApiConfig.sessionById(id));
  }

  /// Add exercise to session
  Future<Exercise> addExerciseToSession(int sessionId, Exercise exercise) async {
    final data = await _apiService.post<Map<String, dynamic>>(
      ApiConfig.sessionExercises(sessionId),
      data: exercise.toJson(),
    );
    return Exercise.fromJson(data);
  }
}
