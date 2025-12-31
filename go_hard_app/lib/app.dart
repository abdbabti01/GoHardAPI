import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'core/theme/app_theme.dart';
import 'routes/app_router.dart';
import 'routes/route_names.dart';
import 'providers/auth_provider.dart';

/// Root application widget
/// Configures navigation, theming, and auth guard
class MyApp extends StatelessWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context) {
    return Consumer<AuthProvider>(
      builder: (context, authProvider, child) {
        return MaterialApp(
          title: 'GoHard - Workout Tracker',
          debugShowCheckedModeBanner: false,

          // Theme configuration
          theme: AppTheme.lightTheme,
          darkTheme: AppTheme.darkTheme,
          themeMode: ThemeMode.system,

          // Navigation configuration
          onGenerateRoute: AppRouter.generateRoute,
          initialRoute: _getInitialRoute(authProvider),

          // Navigator observers for debugging (can be removed in production)
          navigatorObservers: [_RouteObserver()],
        );
      },
    );
  }

  /// Determines initial route based on authentication status
  /// If user is authenticated, go to main screen; otherwise, show login
  String _getInitialRoute(AuthProvider authProvider) {
    if (authProvider.isAuthenticated) {
      return RouteNames.main;
    }
    return RouteNames.login;
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
