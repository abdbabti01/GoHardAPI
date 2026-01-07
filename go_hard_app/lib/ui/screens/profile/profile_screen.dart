import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../../providers/profile_provider.dart';
import '../../../providers/auth_provider.dart';
import '../../../providers/sessions_provider.dart';
import '../../../providers/active_workout_provider.dart';
import '../../../providers/chat_provider.dart';
import '../../../routes/route_names.dart';
import '../../../core/utils/unit_converter.dart';
import '../../../core/constants/api_config.dart';
import 'edit_profile_screen.dart';
import '../goals/goals_screen.dart';
import '../body_metrics/body_metrics_screen.dart';

/// Profile screen for viewing user information and settings
/// Matches ProfilePage.xaml from MAUI app
class ProfileScreen extends StatefulWidget {
  const ProfileScreen({super.key});

  @override
  State<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends State<ProfileScreen> {
  @override
  void initState() {
    super.initState();
    // Load user profile on first build
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<ProfileProvider>().loadUserProfile();
    });
  }

  Future<void> _handleRefresh() async {
    await context.read<ProfileProvider>().loadUserProfile();
  }

  Future<void> _handleLogout() async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder:
          (context) => AlertDialog(
            title: const Text('Logout'),
            content: const Text('Are you sure you want to logout?'),
            actions: [
              TextButton(
                onPressed: () => Navigator.of(context).pop(false),
                child: const Text('Cancel'),
              ),
              ElevatedButton(
                onPressed: () => Navigator.of(context).pop(true),
                style: ElevatedButton.styleFrom(
                  backgroundColor: Colors.red,
                  foregroundColor: Colors.white,
                ),
                child: const Text('Logout'),
              ),
            ],
          ),
    );

    if (confirmed == true && mounted) {
      // Clear all provider states before logout
      context.read<SessionsProvider>().clear();
      context.read<ActiveWorkoutProvider>().clear();
      context.read<ProfileProvider>().clear();
      context.read<ChatProvider>().clear();

      await context.read<AuthProvider>().logout();

      if (mounted) {
        // Navigate to login and clear navigation stack
        Navigator.of(
          context,
        ).pushNamedAndRemoveUntil(RouteNames.login, (route) => false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Profile'),
        actions: [
          IconButton(
            icon: const Icon(Icons.settings),
            onPressed: () {
              Navigator.of(context).pushNamed(RouteNames.settings);
            },
            tooltip: 'Settings',
          ),
          IconButton(
            icon: const Icon(Icons.edit),
            onPressed: () async {
              await Navigator.push(
                context,
                MaterialPageRoute(
                  builder: (context) => const EditProfileScreen(),
                ),
              );
              // Refresh after returning from edit screen
              if (mounted) {
                _handleRefresh();
              }
            },
            tooltip: 'Edit Profile',
          ),
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: _handleRefresh,
            tooltip: 'Refresh',
          ),
        ],
      ),
      body: Consumer<ProfileProvider>(
        builder: (context, provider, child) {
          // Loading state
          if (provider.isLoading && provider.currentUser == null) {
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
                    'Error Loading Profile',
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

          return RefreshIndicator(
            onRefresh: _handleRefresh,
            child: SingleChildScrollView(
              physics: const AlwaysScrollableScrollPhysics(),
              padding: const EdgeInsets.all(16),
              child: Column(
                children: [
                  // Profile header
                  _buildProfileHeader(context, provider),
                  const SizedBox(height: 24),

                  // User info card
                  _buildUserInfoCard(context, provider),
                  const SizedBox(height: 16),

                  // Stats card
                  _buildStatsCard(context, provider),
                  const SizedBox(height: 16),

                  // Tracking menu card
                  _buildTrackingMenuCard(context),
                  const SizedBox(height: 24),

                  // Logout button
                  SizedBox(
                    width: double.infinity,
                    child: ElevatedButton.icon(
                      onPressed: _handleLogout,
                      icon: const Icon(Icons.logout),
                      label: const Text('Logout'),
                      style: ElevatedButton.styleFrom(
                        backgroundColor: Colors.red,
                        foregroundColor: Colors.white,
                        padding: const EdgeInsets.symmetric(vertical: 16),
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12),
                        ),
                      ),
                    ),
                  ),

                  const SizedBox(height: 24),

                  // App info
                  Text(
                    'GoHard - Workout Tracker',
                    style: Theme.of(
                      context,
                    ).textTheme.bodySmall?.copyWith(color: Colors.grey),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    'Version 1.0.0',
                    style: Theme.of(
                      context,
                    ).textTheme.bodySmall?.copyWith(color: Colors.grey),
                  ),
                ],
              ),
            ),
          );
        },
      ),
    );
  }

  Widget _buildProfileHeader(BuildContext context, ProfileProvider provider) {
    final user = provider.currentUser;

    return Column(
      children: [
        // Avatar with profile photo
        CircleAvatar(
          radius: 50,
          backgroundColor: Theme.of(
            context,
          ).colorScheme.primary.withValues(alpha: 0.2),
          backgroundImage:
              user?.profilePhotoUrl != null
                  ? NetworkImage(ApiConfig.getPhotoUrl(user!.profilePhotoUrl!))
                  : null,
          child:
              user?.profilePhotoUrl == null
                  ? Icon(
                    Icons.person,
                    size: 50,
                    color: Theme.of(context).colorScheme.primary,
                  )
                  : null,
        ),
        const SizedBox(height: 16),

        // Name
        FutureBuilder<String>(
          future: provider.getUserName(),
          builder: (context, snapshot) {
            return Text(
              snapshot.data ?? 'Loading...',
              style: Theme.of(
                context,
              ).textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.bold),
            );
          },
        ),
        const SizedBox(height: 4),

        // Email
        FutureBuilder<String>(
          future: provider.getUserEmail(),
          builder: (context, snapshot) {
            return Text(
              snapshot.data ?? '',
              style: Theme.of(
                context,
              ).textTheme.bodyMedium?.copyWith(color: Colors.grey.shade600),
            );
          },
        ),
      ],
    );
  }

  Widget _buildUserInfoCard(BuildContext context, ProfileProvider provider) {
    final user = provider.currentUser;
    final unitPreference = user?.unitPreference ?? 'Metric';

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Profile Information',
              style: Theme.of(
                context,
              ).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 16),

            // Bio
            if (user?.bio != null && user!.bio!.isNotEmpty) ...[
              _buildInfoRow(context, Icons.info, 'Bio', user.bio!),
              const Divider(height: 24),
            ],

            // Age & Date of Birth
            if (user?.age != null) ...[
              _buildInfoRow(
                context,
                Icons.cake,
                'Age',
                '${user!.age} years old',
              ),
              const Divider(height: 24),
            ],

            // Gender
            if (user?.gender != null) ...[
              _buildInfoRow(context, Icons.wc, 'Gender', user!.gender!),
              const Divider(height: 24),
            ],

            // Height with unit conversion
            if (user?.height != null) ...[
              _buildInfoRow(
                context,
                Icons.height,
                'Height',
                UnitConverter.formatHeight(user!.height, unitPreference),
              ),
              const Divider(height: 24),
            ],

            // Weight with unit conversion
            if (user?.weight != null) ...[
              _buildInfoRow(
                context,
                Icons.monitor_weight,
                'Current Weight',
                UnitConverter.formatWeight(user!.weight, unitPreference),
              ),
              const Divider(height: 24),
            ],

            // Target Weight
            if (user?.targetWeight != null) ...[
              _buildInfoRow(
                context,
                Icons.flag,
                'Target Weight',
                UnitConverter.formatWeight(user!.targetWeight, unitPreference),
              ),
              const Divider(height: 24),
            ],

            // Body Fat Percentage
            if (user?.bodyFatPercentage != null) ...[
              _buildInfoRow(
                context,
                Icons.percent,
                'Body Fat',
                '${user!.bodyFatPercentage!.toStringAsFixed(1)}%',
              ),
              const Divider(height: 24),
            ],

            // BMI
            if (user?.bmi != null) ...[
              _buildInfoRow(
                context,
                Icons.analytics,
                'BMI',
                user!.bmi!.toStringAsFixed(1),
              ),
              const Divider(height: 24),
            ],

            // Experience Level
            if (user?.experienceLevel != null) ...[
              _buildInfoRow(
                context,
                Icons.star,
                'Experience Level',
                user!.experienceLevel!,
              ),
              const Divider(height: 24),
            ],

            // Primary Goal
            if (user?.primaryGoal != null) ...[
              _buildInfoRow(
                context,
                Icons.track_changes,
                'Primary Goal',
                user!.primaryGoal!,
              ),
              const Divider(height: 24),
            ],

            // Goals
            if (user?.goals != null && user!.goals!.isNotEmpty) ...[
              _buildInfoRow(context, Icons.description, 'Goals', user.goals!),
              const Divider(height: 24),
            ],

            // Favorite Exercises
            if (user?.favoriteExercises != null &&
                user!.favoriteExercises!.isNotEmpty) ...[
              _buildInfoRow(
                context,
                Icons.favorite,
                'Favorite Exercises',
                user.favoriteExercises!,
              ),
              const Divider(height: 24),
            ],

            // Member since
            _buildInfoRow(
              context,
              Icons.calendar_today,
              'Member Since',
              user?.dateCreated != null
                  ? _formatDate(user!.dateCreated)
                  : 'Unknown',
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildStatsCard(BuildContext context, ProfileProvider provider) {
    final user = provider.currentUser;
    final stats = user?.stats;

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Quick Stats',
              style: Theme.of(
                context,
              ).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 16),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceAround,
              children: [
                _buildStatItem(
                  context,
                  Icons.fitness_center,
                  stats?.totalWorkouts.toString() ?? '-',
                  'Workouts',
                ),
                Container(width: 1, height: 40, color: Colors.grey.shade300),
                _buildStatItem(
                  context,
                  Icons.local_fire_department,
                  stats?.currentStreak.toString() ?? '-',
                  'Streak',
                ),
                Container(width: 1, height: 40, color: Colors.grey.shade300),
                _buildStatItem(
                  context,
                  Icons.trending_up,
                  stats?.personalRecords.toString() ?? '-',
                  'PR\'s',
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildTrackingMenuCard(BuildContext context) {
    return Card(
      child: Column(
        children: [
          ListTile(
            leading: Icon(
              Icons.flag,
              color: Theme.of(context).colorScheme.primary,
            ),
            title: const Text('Goals'),
            subtitle: const Text('Track your fitness goals'),
            trailing: const Icon(Icons.chevron_right),
            onTap: () {
              Navigator.push(
                context,
                MaterialPageRoute(builder: (_) => const GoalsScreen()),
              );
            },
          ),
          const Divider(height: 1),
          ListTile(
            leading: Icon(
              Icons.monitor_weight,
              color: Theme.of(context).colorScheme.primary,
            ),
            title: const Text('Body Metrics'),
            subtitle: const Text('Log and track body measurements'),
            trailing: const Icon(Icons.chevron_right),
            onTap: () {
              Navigator.push(
                context,
                MaterialPageRoute(builder: (_) => const BodyMetricsScreen()),
              );
            },
          ),
        ],
      ),
    );
  }

  Widget _buildInfoRow(
    BuildContext context,
    IconData icon,
    String label,
    String value,
  ) {
    return Row(
      children: [
        Icon(icon, size: 20, color: Theme.of(context).colorScheme.primary),
        const SizedBox(width: 12),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                label,
                style: Theme.of(
                  context,
                ).textTheme.bodySmall?.copyWith(color: Colors.grey.shade600),
              ),
              const SizedBox(height: 2),
              Text(
                value,
                style: Theme.of(
                  context,
                ).textTheme.bodyLarge?.copyWith(fontWeight: FontWeight.w500),
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildStatItem(
    BuildContext context,
    IconData icon,
    String value,
    String label,
  ) {
    return Column(
      children: [
        Icon(icon, size: 32, color: Theme.of(context).colorScheme.primary),
        const SizedBox(height: 8),
        Text(
          value,
          style: Theme.of(
            context,
          ).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 4),
        Text(
          label,
          style: Theme.of(
            context,
          ).textTheme.bodySmall?.copyWith(color: Colors.grey.shade600),
        ),
      ],
    );
  }

  String _formatDate(DateTime date) {
    final months = [
      'Jan',
      'Feb',
      'Mar',
      'Apr',
      'May',
      'Jun',
      'Jul',
      'Aug',
      'Sep',
      'Oct',
      'Nov',
      'Dec',
    ];
    return '${months[date.month - 1]} ${date.day}, ${date.year}';
  }
}
