/// Route name constants for the app
/// Centralizes all navigation route names
class RouteNames {
  // Prevent instantiation
  RouteNames._();

  // Auth routes
  static const String login = '/login';
  static const String signup = '/signup';

  // Main app routes
  static const String main = '/main';

  // Session routes
  static const String sessions = '/sessions';
  static const String sessionDetail = '/session-detail';
  static const String activeWorkout = '/active-workout';

  // Exercise routes
  static const String exercises = '/exercises';
  static const String exerciseDetail = '/exercise-detail';
  static const String addExercise = '/add-exercise';
  static const String logSets = '/log-sets';

  // Profile routes
  static const String profile = '/profile';

  // Analytics routes
  static const String analytics = '/analytics';

  // Initial route (shown when app starts)
  static const String initial = login;
}
