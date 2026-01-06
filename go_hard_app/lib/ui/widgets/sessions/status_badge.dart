import 'package:flutter/material.dart';

/// Status badge widget for workout sessions
/// Displays colored badge based on session status
class StatusBadge extends StatelessWidget {
  final String status;

  const StatusBadge({super.key, required this.status});

  @override
  Widget build(BuildContext context) {
    final badgeData = _getBadgeData(status);

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
      decoration: BoxDecoration(
        color: badgeData.color.withValues(alpha: 0.15),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(
          color: badgeData.color.withValues(alpha: 0.3),
          width: 1,
        ),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(badgeData.icon, size: 14, color: badgeData.color),
          const SizedBox(width: 4),
          Text(
            badgeData.label,
            style: TextStyle(
              color: badgeData.color,
              fontSize: 12,
              fontWeight: FontWeight.w600,
            ),
          ),
        ],
      ),
    );
  }

  _BadgeData _getBadgeData(String status) {
    switch (status.toLowerCase()) {
      case 'completed':
        return _BadgeData(
          label: 'Completed',
          color: Colors.green,
          icon: Icons.check_circle,
        );
      case 'in_progress':
        return _BadgeData(
          label: 'In Progress',
          color: Colors.orange,
          icon: Icons.play_circle_filled,
        );
      case 'planned':
        return _BadgeData(
          label: 'Planned',
          color: Colors.blue,
          icon: Icons.event,
        );
      case 'draft':
        return _BadgeData(label: 'Draft', color: Colors.grey, icon: Icons.edit);
      default:
        return _BadgeData(
          label: status,
          color: Colors.grey,
          icon: Icons.circle,
        );
    }
  }
}

class _BadgeData {
  final String label;
  final Color color;
  final IconData icon;

  _BadgeData({required this.label, required this.color, required this.icon});
}
