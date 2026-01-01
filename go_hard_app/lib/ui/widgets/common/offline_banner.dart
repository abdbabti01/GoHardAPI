import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../../core/services/connectivity_service.dart';

/// Banner widget that displays when the app is offline
/// Shows at the top of the screen to inform users they're working in offline mode
class OfflineBanner extends StatelessWidget {
  const OfflineBanner({super.key});

  @override
  Widget build(BuildContext context) {
    final connectivity = context.watch<ConnectivityService>();

    // Only show banner when offline
    if (connectivity.isOnline) {
      return const SizedBox.shrink();
    }

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      decoration: BoxDecoration(
        color: Colors.orange.shade100,
        border: Border(
          bottom: BorderSide(color: Colors.orange.shade300, width: 1),
        ),
      ),
      child: Row(
        children: [
          Icon(Icons.cloud_off, size: 20, color: Colors.orange.shade700),
          const SizedBox(width: 12),
          Expanded(
            child: Text(
              'You\'re offline - Changes will sync when connected',
              style: TextStyle(
                fontSize: 13,
                color: Colors.orange.shade900,
                fontWeight: FontWeight.w500,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

/// Alternative compact offline indicator for smaller spaces
class CompactOfflineIndicator extends StatelessWidget {
  const CompactOfflineIndicator({super.key});

  @override
  Widget build(BuildContext context) {
    final connectivity = context.watch<ConnectivityService>();

    if (connectivity.isOnline) {
      return const SizedBox.shrink();
    }

    return Container(
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
      decoration: BoxDecoration(
        color: Colors.orange.shade50,
        borderRadius: BorderRadius.circular(4),
        border: Border.all(color: Colors.orange.shade300, width: 1),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(Icons.cloud_off, size: 16, color: Colors.orange.shade700),
          const SizedBox(width: 8),
          Text(
            'Offline mode',
            style: TextStyle(
              fontSize: 12,
              color: Colors.orange.shade900,
              fontWeight: FontWeight.w500,
            ),
          ),
        ],
      ),
    );
  }
}
