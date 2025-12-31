import 'dart:io';

/// API configuration with platform-specific base URLs
/// Matches the ApiService.cs platform detection logic from MAUI app
class ApiConfig {
  ApiConfig._(); // Private constructor to prevent instantiation

  /// Base URL for the GoHardAPI backend
  /// - iOS/macOS: http://localhost:5121/api
  /// - Android: http://10.0.0.4:5121/api (for physical device on same WiFi)
  /// - Other platforms: http://localhost:5121/api
  static String get baseUrl {
    if (Platform.isIOS || Platform.isMacOS) {
      return 'http://localhost:5121/api/';
    } else if (Platform.isAndroid) {
      // IMPORTANT: Choose the correct URL based on your testing device:
      // - Android Emulator: use 10.0.2.2 (special alias to host machine's localhost)
      // - Physical Android Device: use your computer's WiFi IP address (e.g., 10.0.0.4, 192.168.x.x)
      //   To find your IP: Windows (ipconfig), Mac/Linux (ifconfig)
      return 'http://10.0.2.2:5121/api/'; // Currently configured for emulator
    } else {
      // Windows, Linux, or other platforms
      return 'http://localhost:5121/api/';
    }
  }

  /// Connection timeout duration
  static const Duration connectTimeout = Duration(seconds: 10);

  /// Receive timeout duration
  static const Duration receiveTimeout = Duration(seconds: 10);

  /// API endpoints
  static const String authLogin = 'auth/login';
  static const String authSignup = 'auth/signup';
  static const String users = 'users';
  static const String sessions = 'sessions';
  static const String exercises = 'exercises';
  static const String exerciseSets = 'exercisesets';
  static const String exerciseTemplates = 'exercisetemplates';

  /// Helper methods for building endpoint URLs
  static String userById(int id) => '$users/$id';
  static String sessionById(int id) => '$sessions/$id';
  static String sessionStatus(int id) => '$sessions/$id/status';
  static String sessionExercises(int sessionId) =>
      '$sessions/$sessionId/exercises';
  static String exerciseSetById(int id) => '$exerciseSets/$id';
  static String exerciseSetsByExerciseId(int exerciseId) =>
      '$exerciseSets/exercise/$exerciseId';
  static String exerciseSetComplete(int id) => '$exerciseSets/$id/complete';
  static String exerciseTemplateById(int id) => '$exerciseTemplates/$id';
  static String exerciseTemplateCategories = '$exerciseTemplates/categories';
  static String exerciseTemplateMuscleGroups =
      '$exerciseTemplates/musclegroups';
}
