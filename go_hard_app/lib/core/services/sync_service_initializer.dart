import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../providers/auth_provider.dart';
import 'sync_service.dart';

/// Widget that initializes SyncService when user is authenticated
/// and disposes it when user logs out
class SyncServiceInitializer extends StatefulWidget {
  final Widget child;

  const SyncServiceInitializer({super.key, required this.child});

  @override
  State<SyncServiceInitializer> createState() => _SyncServiceInitializerState();
}

class _SyncServiceInitializerState extends State<SyncServiceInitializer> {
  bool _syncInitialized = false;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();

    final authProvider = context.watch<AuthProvider>();
    final syncService = context.read<SyncService>();

    // Initialize sync when user is authenticated
    if (authProvider.isAuthenticated && !_syncInitialized) {
      debugPrint('🔄 Initializing SyncService for authenticated user');
      syncService.initialize();
      _syncInitialized = true;
    }

    // Dispose sync when user logs out
    if (!authProvider.isAuthenticated && _syncInitialized) {
      debugPrint('🛑 Disposing SyncService for logged out user');
      syncService.dispose();
      _syncInitialized = false;
    }
  }

  @override
  Widget build(BuildContext context) {
    return widget.child;
  }
}
