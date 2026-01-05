import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../../providers/sessions_provider.dart';
import '../../../providers/exercises_provider.dart';
import '../../../routes/route_names.dart';
import '../../../core/services/sync_service.dart';
import '../../../core/utils/date_utils.dart';
import '../../widgets/sessions/session_card.dart';
import '../../widgets/sessions/workout_name_dialog.dart';
import '../../widgets/common/offline_banner.dart';

/// Sessions screen displaying list of workout sessions
/// Matches SessionsPage.xaml from MAUI app
class SessionsScreen extends StatefulWidget {
  const SessionsScreen({super.key});

  @override
  State<SessionsScreen> createState() => _SessionsScreenState();
}

class _SessionsScreenState extends State<SessionsScreen> {
  bool _isPlannedExpanded =
      false; // Track if planned workouts section is expanded

  @override
  void initState() {
    super.initState();
    // Load sessions and exercise templates on first build (for offline caching)
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<SessionsProvider>().loadSessions();
      // Trigger exercise templates to load and cache for offline use
      context.read<ExercisesProvider>().loadExercises();
    });
  }

  Future<void> _handleRefresh() async {
    // Trigger manual sync first (will upload pending changes)
    try {
      await context.read<SyncService>().sync();
    } catch (e) {
      debugPrint('Sync failed during refresh: $e');
    }

    // Then reload sessions from local DB (which now includes synced data)
    if (mounted) {
      await context.read<SessionsProvider>().loadSessions();
    }
  }

  Future<void> _handleStartNewWorkout() async {
    // Show dialog to select workout name
    final workoutName = await _showWorkoutNameDialog();

    if (workoutName == null || !mounted) return; // User cancelled or unmounted

    final provider = context.read<SessionsProvider>();
    final session = await provider.startNewWorkout(name: workoutName);

    if (session != null && mounted) {
      // Navigate to active workout screen and reload sessions when returning
      await Navigator.of(
        context,
      ).pushNamed(RouteNames.activeWorkout, arguments: session.id);

      // Reload sessions to reflect any status changes
      if (mounted) {
        await provider.loadSessions();
      }
    }
  }

  Future<String?> _showWorkoutNameDialog() async {
    return showDialog<String>(
      context: context,
      builder: (context) => const WorkoutNameDialog(),
    );
  }

  Future<void> _handleDeleteSession(int sessionId) async {
    await context.read<SessionsProvider>().deleteSession(sessionId);
  }

  Future<void> _handleSessionTap(int sessionId, String status) async {
    if (status == 'planned') {
      // Show dialog to start planned workout
      final shouldStart = await _showStartPlannedWorkoutDialog();
      if (shouldStart == true && mounted) {
        // Start the planned workout (change status to in_progress)
        final provider = context.read<SessionsProvider>();
        final success = await provider.startPlannedWorkout(sessionId);

        if (success && mounted) {
          // Navigate to active workout screen
          await Navigator.of(
            context,
          ).pushNamed(RouteNames.activeWorkout, arguments: sessionId);

          // Reload sessions to reflect any status changes
          if (mounted) {
            await provider.loadSessions();
          }
        }
      }
    } else if (status == 'in_progress' || status == 'draft') {
      // Navigate to active workout screen for in-progress/draft sessions
      await Navigator.of(
        context,
      ).pushNamed(RouteNames.activeWorkout, arguments: sessionId);

      // Reload sessions to reflect any status changes
      if (mounted) {
        await context.read<SessionsProvider>().loadSessions();
      }
    } else {
      // Navigate to detail screen for completed sessions
      Navigator.of(
        context,
      ).pushNamed(RouteNames.sessionDetail, arguments: sessionId);
    }
  }

  Future<bool?> _showStartPlannedWorkoutDialog() async {
    return showDialog<bool>(
      context: context,
      builder:
          (context) => AlertDialog(
            title: const Text('Start Workout'),
            content: const Text(
              'Do you want to start this planned workout now?',
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(context, false),
                child: const Text('Cancel'),
              ),
              ElevatedButton(
                onPressed: () => Navigator.pop(context, true),
                child: const Text('Start'),
              ),
            ],
          ),
    );
  }

  /// Build week header widget
  Widget _buildWeekHeader(String label) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 24, 16, 8),
      child: Text(
        label,
        style: Theme.of(context).textTheme.titleMedium?.copyWith(
          fontWeight: FontWeight.bold,
          color: Theme.of(context).colorScheme.primary,
        ),
      ),
    );
  }

  /// Calculate total items including planned workouts section
  int _calculateTotalItemsWithPlanned(
    List<dynamic> plannedSessions,
    Map<String, List<dynamic>> grouped,
    List<String> labels,
  ) {
    int total = 0;

    // Planned workouts section
    if (plannedSessions.isNotEmpty) {
      total += 1; // Header
      // Only include planned sessions in count if expanded
      if (_isPlannedExpanded) {
        total += plannedSessions.length;
      }
    }

    // Past sessions grouped by week
    for (final label in labels) {
      total += 1; // Header
      total += grouped[label]!.length; // Sessions in this week
    }

    return total;
  }

  /// Build list item including planned workouts section
  Widget _buildListItemWithPlanned(
    int index,
    List<dynamic> plannedSessions,
    Map<String, List<dynamic>> grouped,
    List<String> labels,
  ) {
    int currentIndex = 0;

    // Planned workouts section
    if (plannedSessions.isNotEmpty) {
      // Header for planned workouts
      if (index == currentIndex) {
        return _buildSectionHeader(
          'Planned Workouts',
          Icons.event,
          plannedSessions.length,
        );
      }
      currentIndex++;

      // Planned sessions (only if expanded)
      if (_isPlannedExpanded) {
        if (index < currentIndex + plannedSessions.length) {
          final sessionIndex = index - currentIndex;
          final session = plannedSessions[sessionIndex];
          return SessionCard(
            session: session,
            onTap: () => _handleSessionTap(session.id, session.status),
            onDelete: () => _handleDeleteSession(session.id),
          );
        }
        currentIndex += plannedSessions.length;
      }
    }

    // Past sessions grouped by week
    for (final label in labels) {
      // Check if this index is the header
      if (index == currentIndex) {
        return _buildWeekHeader(label);
      }
      currentIndex++;

      // Check if this index is a session in this week
      final sessionsInWeek = grouped[label]!;
      if (index < currentIndex + sessionsInWeek.length) {
        final sessionIndex = index - currentIndex;
        final session = sessionsInWeek[sessionIndex];
        return SessionCard(
          session: session,
          onTap: () => _handleSessionTap(session.id, session.status),
          onDelete: () => _handleDeleteSession(session.id),
        );
      }
      currentIndex += sessionsInWeek.length;
    }

    // Fallback
    return const SizedBox.shrink();
  }

  /// Build section header widget (for planned workouts)
  Widget _buildSectionHeader(String label, IconData icon, int count) {
    return InkWell(
      onTap: () {
        setState(() {
          _isPlannedExpanded = !_isPlannedExpanded;
        });
      },
      child: Padding(
        padding: const EdgeInsets.fromLTRB(16, 16, 16, 8),
        child: Row(
          children: [
            Icon(icon, size: 20, color: Theme.of(context).colorScheme.primary),
            const SizedBox(width: 8),
            Text(
              label,
              style: Theme.of(context).textTheme.titleMedium?.copyWith(
                fontWeight: FontWeight.bold,
                color: Theme.of(context).colorScheme.primary,
              ),
            ),
            const SizedBox(width: 4),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
              decoration: BoxDecoration(
                color: Theme.of(
                  context,
                ).colorScheme.primary.withValues(alpha: 0.1),
                borderRadius: BorderRadius.circular(12),
              ),
              child: Text(
                '$count',
                style: TextStyle(
                  fontSize: 12,
                  fontWeight: FontWeight.bold,
                  color: Theme.of(context).colorScheme.primary,
                ),
              ),
            ),
            const Spacer(),
            Icon(
              _isPlannedExpanded
                  ? Icons.keyboard_arrow_up
                  : Icons.keyboard_arrow_down,
              color: Theme.of(context).colorScheme.primary,
            ),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Workouts'),
        actions: [
          IconButton(
            icon: const Icon(Icons.analytics),
            onPressed: () {
              Navigator.pushNamed(context, RouteNames.analytics);
            },
            tooltip: 'Analytics',
          ),
        ],
      ),
      body: Column(
        children: [
          const OfflineBanner(),
          Expanded(
            child: Consumer<SessionsProvider>(
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
                            style: Theme.of(context).textTheme.bodyMedium
                                ?.copyWith(color: Colors.grey),
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
                            style: Theme.of(context).textTheme.bodyMedium
                                ?.copyWith(color: Colors.grey),
                          ),
                        ),
                      ],
                    ),
                  );
                }

                // Separate planned and past sessions
                final plannedSessions =
                    provider.sessions
                        .where((s) => s.status == 'planned')
                        .toList();
                final pastSessions =
                    provider.sessions
                        .where((s) => s.status != 'planned')
                        .toList();

                // Group past sessions by week
                final groupedSessions = DateGroupingUtils.groupSessionsByWeek(
                  pastSessions,
                );
                final weekLabels = DateGroupingUtils.getOrderedWeekLabels(
                  groupedSessions,
                );

                return RefreshIndicator(
                  onRefresh: _handleRefresh,
                  child: ListView.builder(
                    physics: const AlwaysScrollableScrollPhysics(),
                    padding: const EdgeInsets.only(top: 8, bottom: 80),
                    itemCount: _calculateTotalItemsWithPlanned(
                      plannedSessions,
                      groupedSessions,
                      weekLabels,
                    ),
                    itemBuilder: (context, index) {
                      return _buildListItemWithPlanned(
                        index,
                        plannedSessions,
                        groupedSessions,
                        weekLabels,
                      );
                    },
                  ),
                );
              },
            ),
          ),
        ],
      ),
      floatingActionButton: Container(
        height: 56,
        decoration: BoxDecoration(
          borderRadius: BorderRadius.circular(28),
          gradient: LinearGradient(
            colors: [
              Theme.of(context).colorScheme.primary,
              Theme.of(context).colorScheme.primary.withValues(alpha: 0.8),
            ],
          ),
          boxShadow: [
            BoxShadow(
              color: Theme.of(
                context,
              ).colorScheme.primary.withValues(alpha: 0.4),
              blurRadius: 12,
              offset: const Offset(0, 4),
            ),
          ],
        ),
        child: Material(
          color: Colors.transparent,
          child: InkWell(
            borderRadius: BorderRadius.circular(28),
            onTap: _handleStartNewWorkout,
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Icon(
                    Icons.fitness_center,
                    color: Theme.of(context).colorScheme.onPrimary,
                    size: 24,
                  ),
                  const SizedBox(width: 12),
                  Text(
                    'New Workout',
                    style: TextStyle(
                      color: Theme.of(context).colorScheme.onPrimary,
                      fontSize: 16,
                      fontWeight: FontWeight.w600,
                      letterSpacing: 0.5,
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
