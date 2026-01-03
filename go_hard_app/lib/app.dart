import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'core/theme/app_theme.dart';
import 'routes/app_router.dart';
import 'providers/auth_provider.dart';
import 'providers/profile_provider.dart';
import 'ui/screens/main_screen.dart';
import 'ui/screens/auth/login_screen.dart';

/// Root application widget
/// Configures navigation, theming, and auth guard
class MyApp extends StatelessWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context) {
    return Consumer2<AuthProvider, ProfileProvider>(
      builder: (context, authProvider, profileProvider, child) {
        return MaterialApp(
          title: 'GoHard - Workout Tracker',
          debugShowCheckedModeBanner: false,

          // Theme configuration - uses user preference
          theme: AppTheme.lightTheme,
          darkTheme: AppTheme.darkTheme,
          themeMode: profileProvider.themeMode,

          // Use home with conditional rendering instead of initialRoute
          home: _buildHome(authProvider),

          // Route generator for named navigation
          onGenerateRoute: AppRouter.generateRoute,

          // Navigator observers for debugging (can be removed in production)
          navigatorObservers: [_RouteObserver()],
        );
      },
    );
  }

  /// Build the appropriate home screen based on auth state
  Widget _buildHome(AuthProvider authProvider) {
    // Show splash screen while checking authentication
    if (authProvider.isInitializing) {
      return const _SplashScreen();
    }

    // Navigate to main or login based on authentication status
    if (authProvider.isAuthenticated) {
      return const MainScreen();
    }

    return const LoginScreen();
  }
}

/// Splash screen shown while checking authentication status
class _SplashScreen extends StatelessWidget {
  const _SplashScreen();

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            // App logo
            Image.asset('assets/logo.png', width: 120, height: 120),
            const SizedBox(height: 24),
            // App name
            Text(
              'GoHard',
              style: Theme.of(
                context,
              ).textTheme.headlineLarge?.copyWith(fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 48),
            // Loading indicator
            const CircularProgressIndicator(),
          ],
        ),
      ),
    );
  }
}

/// Route observer for debugging navigation
/// Can be removed in production builds
class _RouteObserver extends RouteObserver<PageRoute<dynamic>> {
  @override
  void didPush(Route<dynamic> route, Route<dynamic>? previousRoute) {
    super.didPush(route, previousRoute);
    debugPrint('📍 Navigated to: ${route.settings.name}');
  }

  @override
  void didPop(Route<dynamic> route, Route<dynamic>? previousRoute) {
    super.didPop(route, previousRoute);
    debugPrint('📍 Popped from: ${route.settings.name}');
  }
}
