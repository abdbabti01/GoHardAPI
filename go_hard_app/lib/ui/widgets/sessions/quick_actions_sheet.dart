import 'package:flutter/material.dart';
import '../../../data/models/session.dart';

/// Bottom sheet with quick actions for workout sessions
class QuickActionsSheet extends StatelessWidget {
  final Session session;
  final VoidCallback? onReschedule;
  final VoidCallback? onMarkSkipped;
  final VoidCallback? onDuplicate;

  const QuickActionsSheet({
    super.key,
    required this.session,
    this.onReschedule,
    this.onMarkSkipped,
    this.onDuplicate,
  });

  static Future<String?> show(
    BuildContext context,
    Session session, {
    VoidCallback? onReschedule,
    VoidCallback? onMarkSkipped,
    VoidCallback? onDuplicate,
  }) {
    return showModalBottomSheet<String>(
      context: context,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder:
          (context) => QuickActionsSheet(
            session: session,
            onReschedule: onReschedule,
            onMarkSkipped: onMarkSkipped,
            onDuplicate: onDuplicate,
          ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(vertical: 20),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 20),
            child: Row(
              children: [
                Expanded(
                  child: Text(
                    'Quick Actions',
                    style: Theme.of(context).textTheme.titleLarge?.copyWith(
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
                IconButton(
                  icon: const Icon(Icons.close),
                  onPressed: () => Navigator.pop(context),
                ),
              ],
            ),
          ),
          const Divider(),
          // Reschedule action (only for planned workouts)
          if (session.status == 'planned')
            _buildActionItem(
              context,
              icon: Icons.calendar_month,
              title: 'Reschedule',
              subtitle: 'Change the planned date',
              color: Colors.blue,
              onTap: () {
                Navigator.pop(context);
                onReschedule?.call();
              },
            ),

          // Mark as skipped (only for planned workouts)
          if (session.status == 'planned')
            _buildActionItem(
              context,
              icon: Icons.cancel_outlined,
              title: 'Mark as Skipped',
              subtitle: 'Skip this workout',
              color: Colors.orange,
              onTap: () {
                Navigator.pop(context, 'skip');
              },
            ),

          // Duplicate workout
          _buildActionItem(
            context,
            icon: Icons.content_copy,
            title: 'Duplicate',
            subtitle: 'Create a copy of this workout',
            color: Colors.green,
            onTap: () {
              Navigator.pop(context);
              onDuplicate?.call();
            },
          ),

          // Share workout
          _buildActionItem(
            context,
            icon: Icons.share,
            title: 'Share',
            subtitle: 'Share workout details',
            color: Colors.purple,
            onTap: () {
              Navigator.pop(context, 'share');
            },
          ),

          const SizedBox(height: 10),
        ],
      ),
    );
  }

  Widget _buildActionItem(
    BuildContext context, {
    required IconData icon,
    required String title,
    required String subtitle,
    required Color color,
    required VoidCallback onTap,
  }) {
    return InkWell(
      onTap: onTap,
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 12),
        child: Row(
          children: [
            Container(
              width: 48,
              height: 48,
              decoration: BoxDecoration(
                color: color.withValues(alpha: 0.1),
                borderRadius: BorderRadius.circular(12),
              ),
              child: Icon(icon, color: color, size: 24),
            ),
            const SizedBox(width: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    title,
                    style: const TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                  Text(
                    subtitle,
                    style: TextStyle(fontSize: 13, color: Colors.grey.shade600),
                  ),
                ],
              ),
            ),
            Icon(Icons.chevron_right, color: Colors.grey.shade400),
          ],
        ),
      ),
    );
  }
}
