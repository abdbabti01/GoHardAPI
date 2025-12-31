/// Enum representing the synchronization status of a local entity
enum SyncStatus {
  /// Entity is fully synchronized with server
  synced,

  /// Entity created locally, needs to be sent to server
  pendingCreate,

  /// Entity modified locally, needs to be updated on server
  pendingUpdate,

  /// Entity deleted locally, needs to be deleted on server
  pendingDelete,

  /// Sync failed after maximum retries
  syncError,
}

/// Extension to convert SyncStatus to/from string values
extension SyncStatusExtension on SyncStatus {
  /// Convert SyncStatus to string value for database storage
  String get value {
    switch (this) {
      case SyncStatus.synced:
        return 'synced';
      case SyncStatus.pendingCreate:
        return 'pending_create';
      case SyncStatus.pendingUpdate:
        return 'pending_update';
      case SyncStatus.pendingDelete:
        return 'pending_delete';
      case SyncStatus.syncError:
        return 'sync_error';
    }
  }

  /// Parse string value to SyncStatus
  static SyncStatus fromString(String value) {
    switch (value) {
      case 'synced':
        return SyncStatus.synced;
      case 'pending_create':
        return SyncStatus.pendingCreate;
      case 'pending_update':
        return SyncStatus.pendingUpdate;
      case 'pending_delete':
        return SyncStatus.pendingDelete;
      case 'sync_error':
        return SyncStatus.syncError;
      default:
        return SyncStatus.pendingCreate;
    }
  }
}
