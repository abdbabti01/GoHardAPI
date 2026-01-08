import 'package:flutter/material.dart';
import 'route_names.dart';
import '../ui/screens/main_screen.dart';
import '../ui/screens/auth/login_screen.dart';
import '../ui/screens/auth/signup_screen.dart';
import '../ui/screens/sessions/sessions_screen.dart';
import '../ui/screens/sessions/session_detail_screen.dart';
import '../ui/screens/sessions/active_workout_screen.dart';
import '../ui/screens/sessions/planned_workout_form_screen.dart';
import '../ui/screens/exercises/exercises_screen.dart';
import '../ui/screens/exercises/exercise_detail_screen.dart';
import '../ui/screens/exercises/add_exercise_screen.dart';
import '../ui/screens/exercises/log_sets_screen.dart';
import '../ui/screens/profile/profile_screen.dart';
import '../ui/screens/settings/settings_screen.dart';
import '../ui/screens/analytics/analytics_screen.dart';
import '../ui/screens/chat/chat_list_screen.dart';
import '../ui/screens/chat/chat_conversation_screen.dart';
import '../ui/screens/chat/workout_plan_form_screen.dart';
import '../ui/screens/chat/meal_plan_form_screen.dart';
import '../ui/screens/community/community_screen.dart';
import '../ui/screens/templates/templates_screen.dart';

/// Central router for the application
/// Handles route generation and navigation logic
class AppRouter {
  // Prevent instantiation
  AppRouter._();

  /// Generate routes based on route settings
  /// Returns MaterialPageRoute for each named route
  static Route<dynamic> generateRoute(RouteSettings settings) {
    switch (settings.name) {
      // Auth routes
      case RouteNames.login:
        return MaterialPageRoute(
          builder: (_) => const LoginScreen(),
          settings: settings,
        );

      case RouteNames.signup:
        return MaterialPageRoute(
          builder: (_) => const SignupScreen(),
          settings: settings,
        );

      // Main app wrapper with bottom navigation
      case RouteNames.main:
        final initialTab = settings.arguments as int?;
        return MaterialPageRoute(
          builder: (_) => MainScreen(initialTab: initialTab),
          settings: settings,
        );

      // Session routes
      case RouteNames.sessions:
        return MaterialPageRoute(
          builder: (_) => const SessionsScreen(),
          settings: settings,
        );

      case RouteNames.sessionDetail:
        final sessionId = settings.arguments as int?;
        if (sessionId == null) {
          return MaterialPageRoute(
            builder: (_) => const _NotFoundScreen(routeName: 'session-detail'),
          );
        }
        return MaterialPageRoute(
          builder: (_) => SessionDetailScreen(sessionId: sessionId),
          settings: settings,
        );

      case RouteNames.activeWorkout:
        final sessionId = settings.arguments as int?;
        if (sessionId == null) {
          return MaterialPageRoute(
            builder: (_) => const _NotFoundScreen(routeName: 'active-workout'),
          );
        }
        return MaterialPageRoute(
          builder: (_) => ActiveWorkoutScreen(sessionId: sessionId),
          settings: settings,
        );

      case RouteNames.planWorkout:
        return MaterialPageRoute(
          builder: (_) => const PlannedWorkoutFormScreen(),
          settings: settings,
        );

      // Exercise routes
      case RouteNames.exercises:
        return MaterialPageRoute(
          builder: (_) => const ExercisesScreen(),
          settings: settings,
        );

      case RouteNames.exerciseDetail:
        final exerciseId = settings.arguments as int?;
        if (exerciseId == null) {
          return MaterialPageRoute(
            builder: (_) => const _NotFoundScreen(routeName: 'exercise-detail'),
          );
        }
        return MaterialPageRoute(
          builder: (_) => ExerciseDetailScreen(exerciseId: exerciseId),
          settings: settings,
        );

      case RouteNames.addExercise:
        final sessionId = settings.arguments as int?;
        if (sessionId == null) {
          return MaterialPageRoute(
            builder: (_) => const _NotFoundScreen(routeName: 'add-exercise'),
          );
        }
        return MaterialPageRoute(
          builder: (_) => AddExerciseScreen(sessionId: sessionId),
          settings: settings,
        );

      case RouteNames.logSets:
        final exerciseId = settings.arguments as int?;
        if (exerciseId == null) {
          return MaterialPageRoute(
            builder: (_) => const _NotFoundScreen(routeName: 'log-sets'),
          );
        }
        return MaterialPageRoute(
          builder: (_) => LogSetsScreen(exerciseId: exerciseId),
          settings: settings,
        );

      // Profile routes
      case RouteNames.profile:
        return MaterialPageRoute(
          builder: (_) => const ProfileScreen(),
          settings: settings,
        );

      // Settings routes
      case RouteNames.settings:
        return MaterialPageRoute(
          builder: (_) => const SettingsScreen(),
          settings: settings,
        );

      // Analytics routes
      case RouteNames.analytics:
        return MaterialPageRoute(
          builder: (_) => const AnalyticsScreen(),
          settings: settings,
        );

      // Chat routes
      case RouteNames.chatList:
        return MaterialPageRoute(
          builder: (_) => const ChatListScreen(),
          settings: settings,
        );

      case RouteNames.chatConversation:
        final conversationId = settings.arguments as int?;
        if (conversationId == null) {
          return MaterialPageRoute(
            builder:
                (_) => const _NotFoundScreen(routeName: 'chat-conversation'),
          );
        }
        return MaterialPageRoute(
          builder:
              (_) => ChatConversationScreen(conversationId: conversationId),
          settings: settings,
        );

      case RouteNames.workoutPlanForm:
        return MaterialPageRoute(
          builder: (_) => const WorkoutPlanFormScreen(),
          settings: settings,
        );

      case RouteNames.mealPlanForm:
        return MaterialPageRoute(
          builder: (_) => const MealPlanFormScreen(),
          settings: settings,
        );

      // Community routes
      case RouteNames.community:
        return MaterialPageRoute(
          builder: (_) => const CommunityScreen(),
          settings: settings,
        );

      // Template routes
      case RouteNames.templates:
        return MaterialPageRoute(
          builder: (_) => const TemplatesScreen(),
          settings: settings,
        );

      // Unknown route
      default:
        return MaterialPageRoute(
          builder:
              (_) => _NotFoundScreen(routeName: settings.name ?? 'unknown'),
        );
    }
  }
}

/// 404 screen for unknown routes
class _NotFoundScreen extends StatelessWidget {
  final String routeName;

  const _NotFoundScreen({required this.routeName});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Page Not Found')),
      body: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(Icons.error_outline, size: 64, color: Colors.red),
            const SizedBox(height: 16),
            Text('404', style: Theme.of(context).textTheme.headlineLarge),
            const SizedBox(height: 8),
            Text(
              'Route not found: $routeName',
              style: Theme.of(context).textTheme.bodyMedium,
            ),
            const SizedBox(height: 24),
            ElevatedButton(
              onPressed: () {
                Navigator.of(
                  context,
                ).pushNamedAndRemoveUntil(RouteNames.initial, (route) => false);
              },
              child: const Text('Go to Home'),
            ),
          ],
        ),
      ),
    );
  }
}
