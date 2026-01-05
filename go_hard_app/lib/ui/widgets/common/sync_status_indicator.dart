import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../../core/services/sync_service.dart';
import '../../../core/services/connectivity_service.dart';

/// Widget that displays sync status in the app bar
/// Shows online/offline state, pending items count, and sync progress
class SyncStatusIndicator extends StatefulWidget {
  const SyncStatusIndicator({super.key});

  @override
  State<SyncStatusIndicator> createState() => _SyncStatusIndicatorState();
}

class _SyncStatusIndicatorState extends State<SyncStatusIndicator> {
  Map<String, dynamic>? _syncStatus;

  @override
  void initState() {
    super.initState();
    _loadSyncStatus();
  }

  Future<void> _loadSyncStatus() async {
    try {
      final syncService = context.read<SyncService>();
      final status = await syncService.getSyncStatus();
      if (mounted) {
        setState(() {
          _syncStatus = status;
        });
      }
    } catch (e) {
      debugPrint('Error loading sync status: $e');
    }
  }

  @override
  Widget build(BuildContext context) {
    final connectivity = context.watch<ConnectivityService>();

    return GestureDetector(
      onTap: _showSyncStatusDialog,
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 12.0),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            _buildStatusIcon(connectivity.isOnline),
            const SizedBox(width: 4),
            if (_syncStatus != null) _buildStatusText(),
          ],
        ),
      ),
    );
  }

  Widget _buildStatusIcon(bool isOnline) {
    if (_syncStatus?['isSyncing'] == true) {
      return const SizedBox(
        width: 16,
        height: 16,
        child: CircularProgressIndicator(
          strokeWidth: 2,
          valueColor: AlwaysStoppedAnimation<Color>(Colors.blue),
        ),
      );
    }

    return Icon(
      isOnline ? Icons.cloud_done : Icons.cloud_off,
      size: 20,
      color: isOnline ? Colors.green : Colors.grey,
    );
  }

  Widget _buildStatusText() {
    final pendingCount = _syncStatus?['pendingCount'] ?? 0;
    final errorCount = _syncStatus?['errorCount'] ?? 0;
    final isSyncing = _syncStatus?['isSyncing'] ?? false;
    final isOnline = _syncStatus?['isOnline'] ?? false;

    String text;
    Color color;

    if (isSyncing) {
      text = 'Syncing...';
      color = Colors.blue;
    } else if (errorCount > 0) {
      text = '$errorCount error${errorCount > 1 ? 's' : ''}';
      color = Colors.red;
    } else if (pendingCount > 0) {
      text = '$pendingCount pending';
      // Use grey when offline (expected), orange when online (unexpected)
      color = isOnline ? Colors.orange : Colors.grey;
    } else {
      text = 'Synced';
      color = Colors.green;
    }

    return Text(
      text,
      style: TextStyle(fontSize: 12, color: color, fontWeight: FontWeight.w500),
    );
  }

  void _showSyncStatusDialog() {
    showDialog(
      context: context,
      builder: (context) => _SyncStatusDialog(syncStatus: _syncStatus),
    );
  }
}

/// Dialog showing detailed sync status
class _SyncStatusDialog extends StatelessWidget {
  final Map<String, dynamic>? syncStatus;

  const _SyncStatusDialog({required this.syncStatus});

  @override
  Widget build(BuildContext context) {
    final pendingCount = syncStatus?['pendingCount'] ?? 0;
    final errorCount = syncStatus?['errorCount'] ?? 0;
    final isSyncing = syncStatus?['isSyncing'] ?? false;
    final isOnline = syncStatus?['isOnline'] ?? false;
    final lastSyncTime = syncStatus?['lastSyncTime'] as DateTime?;

    return AlertDialog(
      title: Row(
        children: [
          Icon(
            isOnline ? Icons.cloud_done : Icons.cloud_off,
            color: isOnline ? Colors.green : Colors.grey,
          ),
          const SizedBox(width: 8),
          Text(isOnline ? 'Online' : 'Offline'),
        ],
      ),
      content: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _buildStatusRow(
            'Status',
            isSyncing
                ? 'Syncing...'
                : pendingCount > 0
                ? 'Pending sync'
                : 'Up to date',
            isSyncing
                ? Colors.blue
                : pendingCount > 0
                ? (isOnline ? Colors.orange : Colors.grey)
                : Colors.green,
          ),
          const SizedBox(height: 8),
          _buildStatusRow(
            'Pending items',
            '$pendingCount',
            pendingCount > 0
                ? (isOnline ? Colors.orange : Colors.grey)
                : Colors.grey,
          ),
          const SizedBox(height: 8),
          _buildStatusRow(
            'Errors',
            '$errorCount',
            errorCount > 0 ? Colors.red : Colors.grey,
          ),
          const SizedBox(height: 8),
          _buildStatusRow(
            'Last sync',
            lastSyncTime != null ? _formatLastSync(lastSyncTime) : 'Never',
            Colors.grey,
          ),
        ],
      ),
      actions: [
        if (errorCount > 0)
          TextButton(
            onPressed: () {
              Navigator.pop(context);
              _retryFailedSyncs(context);
            },
            child: const Text('Retry Failed'),
          ),
        if (pendingCount > 0 && isOnline)
          TextButton(
            onPressed: () {
              Navigator.pop(context);
              _triggerManualSync(context);
            },
            child: const Text('Sync Now'),
          ),
        TextButton(
          onPressed: () => Navigator.pop(context),
          child: const Text('Close'),
        ),
      ],
    );
  }

  Widget _buildStatusRow(String label, String value, Color valueColor) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(label, style: const TextStyle(fontWeight: FontWeight.w500)),
        Text(value, style: TextStyle(color: valueColor)),
      ],
    );
  }

  String _formatLastSync(DateTime lastSync) {
    final now = DateTime.now();
    final difference = now.difference(lastSync);

    if (difference.inSeconds < 60) {
      return 'Just now';
    } else if (difference.inMinutes < 60) {
      return '${difference.inMinutes}m ago';
    } else if (difference.inHours < 24) {
      return '${difference.inHours}h ago';
    } else {
      return '${difference.inDays}d ago';
    }
  }

  Future<void> _triggerManualSync(BuildContext context) async {
    try {
      final syncService = context.read<SyncService>();
      final scaffoldMessenger = ScaffoldMessenger.of(context);

      scaffoldMessenger.showSnackBar(
        const SnackBar(content: Text('Starting sync...')),
      );

      await syncService.sync();

      if (context.mounted) {
        scaffoldMessenger.showSnackBar(
          const SnackBar(
            content: Text('Sync completed successfully'),
            backgroundColor: Colors.green,
          ),
        );
      }
    } catch (e) {
      if (context.mounted) {
        final scaffoldMessenger = ScaffoldMessenger.of(context);
        scaffoldMessenger.showSnackBar(
          SnackBar(
            content: Text('Sync failed: $e'),
            backgroundColor: Colors.red,
          ),
        );
      }
    }
  }

  Future<void> _retryFailedSyncs(BuildContext context) async {
    try {
      final syncService = context.read<SyncService>();
      final scaffoldMessenger = ScaffoldMessenger.of(context);

      scaffoldMessenger.showSnackBar(
        const SnackBar(content: Text('Retrying failed syncs...')),
      );

      await syncService.retryFailedSyncs();

      if (context.mounted) {
        scaffoldMessenger.showSnackBar(
          const SnackBar(
            content: Text('Retry completed'),
            backgroundColor: Colors.green,
          ),
        );
      }
    } catch (e) {
      if (context.mounted) {
        final scaffoldMessenger = ScaffoldMessenger.of(context);
        scaffoldMessenger.showSnackBar(
          SnackBar(
            content: Text('Retry failed: $e'),
            backgroundColor: Colors.red,
          ),
        );
      }
    }
  }
}
