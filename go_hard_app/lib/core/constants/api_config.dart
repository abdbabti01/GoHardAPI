/// API configuration with platform-specific base URLs
/// Matches the ApiService.cs platform detection logic from MAUI app
class ApiConfig {
  ApiConfig._(); // Private constructor to prevent instantiation

  /// Base URL for the GoHardAPI backend
  /// Production: https://gohardapi-production.up.railway.app/api
  static String get baseUrl {
    // Production URL - hosted on Railway
    return 'https://gohardapi-production.up.railway.app/api/';

    // For local development, uncomment the appropriate line below:
    // iOS/macOS: return 'http://localhost:5121/api/';
    // Android Emulator: return 'http://10.0.2.2:5121/api/';
    // Physical device on WiFi: return 'http://YOUR_IP:5121/api/';
  }

  /// Server URL without /api suffix (for static files like profile photos)
  static String get serverUrl {
    // Production URL - hosted on Railway
    return 'https://gohardapi-production.up.railway.app';

    // For local development, uncomment the appropriate line below:
    // iOS/macOS: return 'http://localhost:5121';
    // Android Emulator: return 'http://10.0.2.2:5121';
    // Physical device on WiFi: return 'http://YOUR_IP:5121';
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
  static const String sharedWorkouts = 'sharedworkouts';
  static const String workoutTemplates = 'workouttemplates';
  static const String goals = 'goals';
  static const String bodyMetrics = 'bodymetrics';

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
  static String goalById(int id) => '$goals/$id';
  static String goalComplete(int id) => '$goals/$id/complete';
  static String goalProgress(int id) => '$goals/$id/progress';
  static String goalHistory(int id) => '$goals/$id/history';
  static String bodyMetricById(int id) => '$bodyMetrics/$id';
  static String bodyMetricsLatest = '$bodyMetrics/latest';
  static String bodyMetricsChart = '$bodyMetrics/chart';
}
