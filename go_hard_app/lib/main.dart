import 'package:flutter/material.dart';
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
import 'data/local/services/local_database_service.dart';
import 'core/services/connectivity_service.dart';
import 'core/services/sync_service.dart';
import 'core/services/sync_service_initializer.dart';
import 'core/utils/database_cleanup.dart';
import 'providers/auth_provider.dart';
import 'providers/sessions_provider.dart';
import 'providers/active_workout_provider.dart';
import 'providers/exercises_provider.dart';
import 'providers/exercise_detail_provider.dart';
import 'providers/log_sets_provider.dart';
import 'providers/profile_provider.dart';
import 'providers/analytics_provider.dart';
import 'providers/music_player_provider.dart';

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
  runApp(
    /// MultiProvider setup for dependency injection and state management
    /// Matches the service and ViewModel structure from MAUI app
    MultiProvider(
      providers: [
        // Services (singletons)
        Provider<LocalDatabaseService>.value(value: localDb),
        ChangeNotifierProvider<ConnectivityService>.value(value: connectivity),
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
        ProxyProvider2<ApiService, AuthService, ProfileRepository>(
          update:
              (_, apiService, authService, __) =>
                  ProfileRepository(apiService, authService),
        ),
        ProxyProvider<ApiService, AnalyticsRepository>(
          update: (_, apiService, __) => AnalyticsRepository(apiService),
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
        ChangeNotifierProxyProvider<SessionRepository, ActiveWorkoutProvider>(
          create:
              (context) =>
                  ActiveWorkoutProvider(context.read<SessionRepository>()),
          update:
              (_, sessionRepo, previous) =>
                  previous ?? ActiveWorkoutProvider(sessionRepo),
        ),
        ChangeNotifierProxyProvider<ExerciseRepository, ExercisesProvider>(
          create:
              (context) =>
                  ExercisesProvider(context.read<ExerciseRepository>()),
          update:
              (_, exerciseRepo, previous) =>
                  previous ?? ExercisesProvider(exerciseRepo),
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
        ChangeNotifierProxyProvider2<
          ProfileRepository,
          AuthService,
          ProfileProvider
        >(
          create:
              (context) => ProfileProvider(
                context.read<ProfileRepository>(),
                context.read<AuthService>(),
              ),
          update:
              (_, profileRepo, authService, previous) =>
                  previous ?? ProfileProvider(profileRepo, authService),
        ),
        ChangeNotifierProxyProvider<AnalyticsRepository, AnalyticsProvider>(
          create:
              (context) =>
                  AnalyticsProvider(context.read<AnalyticsRepository>()),
          update:
              (_, analyticsRepo, previous) =>
                  previous ?? AnalyticsProvider(analyticsRepo),
        ),
        ChangeNotifierProvider<MusicPlayerProvider>(
          create: (_) => MusicPlayerProvider(),
        ),
      ],
      child: const SyncServiceInitializer(child: MyApp()),
    ),
  );
}
