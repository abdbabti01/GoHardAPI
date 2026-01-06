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
      // For physical iPhone on WiFi, use computer's IP address
      return 'http://10.0.0.4:5121/api/';
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

  /// Server URL without /api suffix (for static files like profile photos)
  static String get serverUrl {
    if (Platform.isIOS || Platform.isMacOS) {
      return 'http://10.0.0.4:5121';
    } else if (Platform.isAndroid) {
      return 'http://10.0.2.2:5121';
    } else {
      return 'http://localhost:5121';
    }
  }

  /// Get full URL for a profile photo
  /// Converts relative paths like '/uploads/profiles/user_5.jpg'
  /// to full URLs like 'http://10.0.2.2:5121/uploads/profiles/user_5.jpg'
  static String getPhotoUrl(String? relativePath) {
    if (relativePath == null || relativePath.isEmpty) return '';
    // If it's already a full URL, return as-is
    if (relativePath.startsWith('http')) return relativePath;
    // Otherwise, prepend server URL
    return '$serverUrl$relativePath';
  }

  /// Connection timeout duration
  /// Reduced to 3 seconds for faster offline detection
  static const Duration connectTimeout = Duration(seconds: 3);

  /// Receive timeout duration
  /// Set to 180 seconds (3 minutes) to accommodate AI-generated responses (workout plans, etc.)
  /// AI models like Claude can take 60-120 seconds for complex workout plan generation
  static const Duration receiveTimeout = Duration(seconds: 180);

  /// API endpoints
  static const String authLogin = 'auth/login';
  static const String authSignup = 'auth/signup';
  static const String users = 'users';
  static const String sessions = 'sessions';
  static const String exercises = 'exercises';
  static const String exerciseSets = 'exercisesets';
  static const String exerciseTemplates = 'exercisetemplates';
  static const String profile = 'profile';
  static const String profilePhoto = 'profile/photo';
  static const String chatConversations = 'chat/conversations';
  static const String chatWorkoutPlan = 'chat/workout-plan';
  static const String chatMealPlan = 'chat/meal-plan';
  static const String chatAnalyzeProgress = 'chat/analyze-progress';

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
  static String chatConversationById(int id) => '$chatConversations/$id';
  static String chatMessages(int conversationId) =>
      '$chatConversations/$conversationId/messages';
  static String chatMessagesStream(int conversationId) =>
      '$chatConversations/$conversationId/messages/stream';
  static String chatPreviewSessions(int conversationId) =>
      '$chatConversations/$conversationId/preview-sessions';
  static String chatCreateSessions(int conversationId) =>
      '$chatConversations/$conversationId/create-sessions';
}
