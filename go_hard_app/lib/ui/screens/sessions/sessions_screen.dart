import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../../providers/sessions_provider.dart';
import '../../../routes/route_names.dart';
import '../../widgets/sessions/session_card.dart';

/// Sessions screen displaying list of workout sessions
/// Matches SessionsPage.xaml from MAUI app
class SessionsScreen extends StatefulWidget {
  const SessionsScreen({super.key});

  @override
  State<SessionsScreen> createState() => _SessionsScreenState();
}

class _SessionsScreenState extends State<SessionsScreen> {
  @override
  void initState() {
    super.initState();
    // Load sessions on first build
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<SessionsProvider>().loadSessions();
    });
  }

  Future<void> _handleRefresh() async {
    await context.read<SessionsProvider>().loadSessions();
  }

  Future<void> _handleStartNewWorkout() async {
    final provider = context.read<SessionsProvider>();
    final session = await provider.startNewWorkout();

    if (session != null && mounted) {
      // Navigate to active workout screen
      Navigator.of(
        context,
      ).pushNamed(RouteNames.activeWorkout, arguments: session.id);
    }
  }

  Future<void> _handleDeleteSession(int sessionId) async {
    await context.read<SessionsProvider>().deleteSession(sessionId);
  }

  void _handleSessionTap(int sessionId, String status) {
    if (status == 'in_progress' || status == 'draft') {
      // Navigate to active workout screen for in-progress/draft sessions
      Navigator.of(
        context,
      ).pushNamed(RouteNames.activeWorkout, arguments: sessionId);
    } else {
      // Navigate to detail screen for completed sessions
      Navigator.of(
        context,
      ).pushNamed(RouteNames.sessionDetail, arguments: sessionId);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Workouts'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _handleRefresh,
            tooltip: 'Refresh',
          ),
        ],
      ),
      body: Consumer<SessionsProvider>(
        builder: (context, provider, child) {
          // Loading state
          if (provider.isLoading && provider.sessions.isEmpty) {
            return const Center(child: CircularProgressIndicator());
          }

          // Error state
          if (provider.errorMessage != null &&
              provider.errorMessage!.isNotEmpty) {
            return Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(
                    Icons.error_outline,
                    size: 64,
                    color: Colors.red.shade300,
                  ),
                  const SizedBox(height: 16),
                  Text(
                    'Error Loading Workouts',
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                  const SizedBox(height: 8),
                  Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 32),
                    child: Text(
                      provider.errorMessage!,
                      textAlign: TextAlign.center,
                      style: Theme.of(
                        context,
                      ).textTheme.bodyMedium?.copyWith(color: Colors.grey),
                    ),
                  ),
                  const SizedBox(height: 24),
                  ElevatedButton.icon(
                    onPressed: _handleRefresh,
                    icon: const Icon(Icons.refresh),
                    label: const Text('Retry'),
                  ),
                ],
              ),
            );
          }

          // Empty state
          if (provider.sessions.isEmpty) {
            return Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(
                    Icons.fitness_center,
                    size: 80,
                    color: Colors.grey.shade300,
                  ),
                  const SizedBox(height: 16),
                  Text(
                    'No Workouts Yet',
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                  const SizedBox(height: 8),
                  Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 48),
                    child: Text(
                      'Start your first workout by tapping the + button below',
                      textAlign: TextAlign.center,
                      style: Theme.of(
                        context,
                      ).textTheme.bodyMedium?.copyWith(color: Colors.grey),
                    ),
                  ),
                ],
              ),
            );
          }

          // Sessions list with pull-to-refresh
          return RefreshIndicator(
            onRefresh: _handleRefresh,
            child: ListView.builder(
              physics: const AlwaysScrollableScrollPhysics(),
              itemCount: provider.sessions.length,
              padding: const EdgeInsets.only(top: 8, bottom: 80),
              itemBuilder: (context, index) {
                final session = provider.sessions[index];
                return SessionCard(
                  session: session,
                  onTap: () => _handleSessionTap(session.id, session.status),
                  onDelete: () => _handleDeleteSession(session.id),
                );
              },
            ),
          );
        },
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: _handleStartNewWorkout,
        icon: const Icon(Icons.add),
        label: const Text('New Workout'),
      ),
    );
  }
}
