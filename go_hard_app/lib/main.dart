import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:provider/provider.dart';

import 'app.dart';
import 'data/services/auth_service.dart';
import 'data/services/api_service.dart';
import 'data/repositories/auth_repository.dart';
import 'data/repositories/session_repository.dart';
import 'data/repositories/exercise_repository.dart';
import 'data/repositories/user_repository.dart';
import 'data/repositories/profile_repository.dart';
import 'data/repositories/analytics_repository.dart';
import 'data/repositories/chat_repository.dart';
import 'data/repositories/shared_workout_repository.dart';
import 'data/repositories/workout_template_repository.dart';
import 'data/repositories/goals_repository.dart';
import 'data/repositories/body_metrics_repository.dart';
import 'data/local/services/local_database_service.dart';
import 'core/services/connectivity_service.dart';
import 'core/services/sync_service.dart';
import 'core/services/sync_service_initializer.dart';
import 'core/services/notification_service.dart';
import 'core/utils/database_cleanup.dart';
import 'providers/auth_provider.dart';
import 'providers/sessions_provider.dart';
import 'providers/active_workout_provider.dart';
import 'providers/exercises_provider.dart';
import 'providers/exercise_detail_provider.dart';
import 'providers/log_sets_provider.dart';
import 'providers/profile_provider.dart';
import 'providers/analytics_provider.dart';
import 'providers/chat_provider.dart';
import 'providers/shared_workout_provider.dart';
import 'providers/workout_template_provider.dart';
import 'providers/goals_provider.dart';
import 'providers/body_metrics_provider.dart';
import 'providers/music_player_provider.dart';
import 'providers/settings_provider.dart';

void main() async {
  // Ensure Flutter bindings are initialized for async operations
  WidgetsFlutterBinding.ensureInitialized();

  // Initialize local database before app starts
  final localDb = LocalDatabaseService.instance;
  await localDb.initialize();

  debugPrint('✅ Local database initialized successfully');
  debugPrint('📊 Database path: ${localDb.database.directory}');
  debugPrint('🔍 Isar Inspector enabled - use Isar Inspector app to view data');

  // Clean up failed/corrupted sessions on startup
  await DatabaseCleanup.cleanupFailedSessions(localDb.database);

  // Initialize connectivity service
  final connectivity = ConnectivityService.instance;
  await connectivity.initialize();

  // Initialize notification service
  final notificationService = NotificationService();
  await notificationService.initialize();

  // Initialize secure storage
  const secureStorage = FlutterSecureStorage();
  runApp(
    /// MultiProvider setup for dependency injection and state management
    /// Matches the service and ViewModel structure from MAUI app
    MultiProvider(
      providers: [
        // Services (singletons)
        Provider<LocalDatabaseService>.value(value: localDb),
        ChangeNotifierProvider<ConnectivityService>.value(value: connectivity),
        Provider<NotificationService>.value(value: notificationService),
        Provider<FlutterSecureStorage>.value(value: secureStorage),
        Provider<AuthService>(create: (_) => AuthService()),
        ProxyProvider<AuthService, ApiService>(
          update: (_, authService, __) => ApiService(authService),
        ),

        // Repositories
        ProxyProvider<ApiService, AuthRepository>(
          update: (_, apiService, __) => AuthRepository(apiService),
        ),
        ProxyProvider4<
          ApiService,
          LocalDatabaseService,
          ConnectivityService,
          AuthService,
          SessionRepository
        >(
          update:
              (_, apiService, localDb, connectivity, authService, __) =>
                  SessionRepository(
                    apiService,
                    localDb,
                    connectivity,
                    authService,
                  ),
        ),
        ProxyProvider3<
          ApiService,
          LocalDatabaseService,
          ConnectivityService,
          ExerciseRepository
        >(
          update:
              (_, apiService, localDb, connectivity, __) =>
                  ExerciseRepository(apiService, localDb, connectivity),
        ),
        ProxyProvider<ApiService, UserRepository>(
          update: (_, apiService, __) => UserRepository(apiService),
        ),
        ProxyProvider3<
          ApiService,
          AuthService,
          ConnectivityService,
          ProfileRepository
        >(
          update:
              (_, apiService, authService, connectivity, __) =>
                  ProfileRepository(apiService, authService, connectivity),
        ),
        ProxyProvider4<
          ApiService,
          LocalDatabaseService,
          ConnectivityService,
          AuthService,
          AnalyticsRepository
        >(
          update:
              (_, apiService, localDb, connectivity, authService, __) =>
                  AnalyticsRepository(
                    apiService,
                    localDb,
                    connectivity,
                    authService,
                  ),
        ),
        ProxyProvider4<
          ApiService,
          LocalDatabaseService,
          ConnectivityService,
          AuthService,
          ChatRepository
        >(
          update:
              (_, apiService, localDb, connectivity, authService, __) =>
                  ChatRepository(
                    apiService,
                    localDb,
                    connectivity,
                    authService,
                  ),
        ),
        ProxyProvider4<
          ApiService,
          LocalDatabaseService,
          ConnectivityService,
          AuthService,
          SharedWorkoutRepository
        >(
          update:
              (_, apiService, localDb, connectivity, authService, __) =>
                  SharedWorkoutRepository(
                    apiService,
                    localDb,
                    connectivity,
                    authService,
                  ),
        ),
        ProxyProvider4<
          ApiService,
          LocalDatabaseService,
          ConnectivityService,
          AuthService,
          WorkoutTemplateRepository
        >(
          update:
              (_, apiService, localDb, connectivity, authService, __) =>
                  WorkoutTemplateRepository(
                    apiService,
                    localDb,
                    connectivity,
                    authService,
                  ),
        ),
        ProxyProvider2<ApiService, ConnectivityService, GoalsRepository>(
          update:
              (_, apiService, connectivity, __) =>
                  GoalsRepository(apiService, connectivity),
        ),
        ProxyProvider2<ApiService, ConnectivityService, BodyMetricsRepository>(
          update:
              (_, apiService, connectivity, __) =>
                  BodyMetricsRepository(apiService, connectivity),
        ),

        // Sync Service
        ProxyProvider4<
          ApiService,
          AuthService,
          LocalDatabaseService,
          ConnectivityService,
          SyncService
        >(
          update:
              (_, apiService, authService, localDb, connectivity, __) =>
                  SyncService(
                    apiService: apiService,
                    authService: authService,
                    localDb: localDb,
                    connectivity: connectivity,
                  ),
        ),

        // Providers (state managers - equivalent to ViewModels)
        ChangeNotifierProxyProvider3<
          AuthRepository,
          AuthService,
          LocalDatabaseService,
          AuthProvider
        >(
          create:
              (context) => AuthProvider(
                context.read<AuthRepository>(),
                context.read<AuthService>(),
                context.read<LocalDatabaseService>(),
              ),
          update:
              (_, authRepo, authService, localDb, previous) =>
                  previous ?? AuthProvider(authRepo, authService, localDb),
        ),
        ChangeNotifierProxyProvider3<
          SessionRepository,
          AuthService,
          ConnectivityService,
          SessionsProvider
        >(
          create:
              (context) => SessionsProvider(
                context.read<SessionRepository>(),
                context.read<AuthService>(),
                context.read<ConnectivityService>(),
              ),
          update:
              (_, sessionRepo, authService, connectivity, previous) =>
                  previous ??
                  SessionsProvider(sessionRepo, authService, connectivity),
        ),
        ChangeNotifierProxyProvider2<
          SessionRepository,
          ConnectivityService,
          ActiveWorkoutProvider
        >(
          create:
              (context) => ActiveWorkoutProvider(
                context.read<SessionRepository>(),
                context.read<ConnectivityService>(),
              ),
          update:
              (_, sessionRepo, connectivity, previous) =>
                  previous ?? ActiveWorkoutProvider(sessionRepo, connectivity),
        ),
        ChangeNotifierProxyProvider2<
          ExerciseRepository,
          ConnectivityService,
          ExercisesProvider
        >(
          create:
              (context) => ExercisesProvider(
                context.read<ExerciseRepository>(),
                context.read<ConnectivityService>(),
              ),
          update:
              (_, exerciseRepo, connectivity, previous) =>
                  previous ?? ExercisesProvider(exerciseRepo, connectivity),
        ),
        ChangeNotifierProxyProvider<ExerciseRepository, ExerciseDetailProvider>(
          create:
              (context) =>
                  ExerciseDetailProvider(context.read<ExerciseRepository>()),
          update:
              (_, exerciseRepo, previous) =>
                  previous ?? ExerciseDetailProvider(exerciseRepo),
        ),
        ChangeNotifierProxyProvider<ExerciseRepository, LogSetsProvider>(
          create:
              (context) => LogSetsProvider(context.read<ExerciseRepository>()),
          update:
              (_, exerciseRepo, previous) =>
                  previous ?? LogSetsProvider(exerciseRepo),
        ),
        ChangeNotifierProxyProvider3<
          ProfileRepository,
          AuthService,
          ConnectivityService,
          ProfileProvider
        >(
          create:
              (context) => ProfileProvider(
                context.read<ProfileRepository>(),
                context.read<AuthService>(),
                context.read<ConnectivityService>(),
              ),
          update:
              (_, profileRepo, authService, connectivity, previous) =>
                  previous ??
                  ProfileProvider(profileRepo, authService, connectivity),
        ),
        ChangeNotifierProxyProvider<AnalyticsRepository, AnalyticsProvider>(
          create:
              (context) =>
                  AnalyticsProvider(context.read<AnalyticsRepository>()),
          update:
              (_, analyticsRepo, previous) =>
                  previous ?? AnalyticsProvider(analyticsRepo),
        ),
        ChangeNotifierProxyProvider2<
          ChatRepository,
          ConnectivityService,
          ChatProvider
        >(
          create:
              (context) => ChatProvider(
                context.read<ChatRepository>(),
                context.read<ConnectivityService>(),
              ),
          update:
              (_, chatRepo, connectivity, previous) =>
                  previous ?? ChatProvider(chatRepo, connectivity),
        ),
        ChangeNotifierProxyProvider2<
          SharedWorkoutRepository,
          ConnectivityService,
          SharedWorkoutProvider
        >(
          create:
              (context) => SharedWorkoutProvider(
                context.read<SharedWorkoutRepository>(),
                context.read<ConnectivityService>(),
              ),
          update:
              (_, sharedWorkoutRepo, connectivity, previous) =>
                  previous ??
                  SharedWorkoutProvider(sharedWorkoutRepo, connectivity),
        ),
        ChangeNotifierProxyProvider2<
          WorkoutTemplateRepository,
          ConnectivityService,
          WorkoutTemplateProvider
        >(
          create:
              (context) => WorkoutTemplateProvider(
                context.read<WorkoutTemplateRepository>(),
                context.read<ConnectivityService>(),
              ),
          update:
              (_, workoutTemplateRepo, connectivity, previous) =>
                  previous ??
                  WorkoutTemplateProvider(workoutTemplateRepo, connectivity),
        ),
        ChangeNotifierProxyProvider2<
          GoalsRepository,
          ConnectivityService,
          GoalsProvider
        >(
          create:
              (context) => GoalsProvider(
                context.read<GoalsRepository>(),
                context.read<ConnectivityService>(),
              ),
          update:
              (_, goalsRepo, connectivity, previous) =>
                  previous ?? GoalsProvider(goalsRepo, connectivity),
        ),
        ChangeNotifierProxyProvider2<
          BodyMetricsRepository,
          ConnectivityService,
          BodyMetricsProvider
        >(
          create:
              (context) => BodyMetricsProvider(
                context.read<BodyMetricsRepository>(),
                context.read<ConnectivityService>(),
              ),
          update:
              (_, bodyMetricsRepo, connectivity, previous) =>
                  previous ??
                  BodyMetricsProvider(bodyMetricsRepo, connectivity),
        ),
        ChangeNotifierProvider<MusicPlayerProvider>(
          create: (_) => MusicPlayerProvider(),
        ),
        ChangeNotifierProxyProvider2<
          FlutterSecureStorage,
          NotificationService,
          SettingsProvider
        >(
          create:
              (context) => SettingsProvider(
                context.read<FlutterSecureStorage>(),
                context.read<NotificationService>(),
              ),
          update:
              (_, storage, notificationService, previous) =>
                  previous ?? SettingsProvider(storage, notificationService),
        ),
      ],
      child: const SyncServiceInitializer(child: MyApp()),
    ),
  );
}
