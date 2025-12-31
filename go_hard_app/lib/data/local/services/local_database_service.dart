import 'package:isar/isar.dart';
import 'package:path_provider/path_provider.dart';
import '../models/local_session.dart';
import '../models/local_exercise.dart';
import '../models/local_exercise_set.dart';
import '../models/local_exercise_template.dart';

/// Service for managing the local Isar database
class LocalDatabaseService {
  static LocalDatabaseService? _instance;
  Isar? _isar;

  /// Private constructor for singleton pattern
  LocalDatabaseService._();

  /// Get singleton instance
  static LocalDatabaseService get instance {
    _instance ??= LocalDatabaseService._();
    return _instance!;
  }

  /// Initialize the Isar database
  /// Returns the initialized Isar instance
  Future<Isar> initialize() async {
    if (_isar != null && _isar!.isOpen) {
      return _isar!;
    }

    // Get application documents directory
    final dir = await getApplicationDocumentsDirectory();

    // Open Isar database with all schemas
    _isar = await Isar.open(
      [
        LocalSessionSchema,
        LocalExerciseSchema,
        LocalExerciseSetSchema,
        LocalExerciseTemplateSchema,
      ],
      directory: dir.path,
      name: 'go_hard_local_db',
      inspector: true, // Enable Isar Inspector for debugging
    );

    return _isar!;
  }

  /// Get the current database instance
  /// Throws if database is not initialized
  Isar get database {
    if (_isar == null || !_isar!.isOpen) {
      throw Exception(
        'Database not initialized. Call initialize() first.',
      );
    }
    return _isar!;
  }

  /// Check if database is initialized
  bool get isInitialized => _isar != null && _isar!.isOpen;

  /// Close the database
  Future<void> close() async {
    if (_isar != null && _isar!.isOpen) {
      await _isar!.close();
      _isar = null;
    }
  }

  /// Clear all data from the database (useful for testing/logout)
  Future<void> clearAll() async {
    if (_isar != null && _isar!.isOpen) {
      await _isar!.writeTxn(() async {
        await _isar!.clear();
      });
    }
  }

  /// Get count of pending sync items across all collections
  Future<int> getPendingSyncCount() async {
    if (_isar == null || !_isar!.isOpen) return 0;

    final sessionCount = await _isar!.localSessions
        .filter()
        .isSyncedEqualTo(false)
        .count();

    final exerciseCount = await _isar!.localExercises
        .filter()
        .isSyncedEqualTo(false)
        .count();

    final setCount = await _isar!.localExerciseSets
        .filter()
        .isSyncedEqualTo(false)
        .count();

    final templateCount = await _isar!.localExerciseTemplates
        .filter()
        .isSyncedEqualTo(false)
        .count();

    return sessionCount + exerciseCount + setCount + templateCount;
  }

  /// Get last sync time across all collections
  Future<DateTime?> getLastSyncTime() async {
    if (_isar == null || !_isar!.isOpen) return null;

    final sessions = await _isar!.localSessions
        .where()
        .sortByLastSyncAttemptDesc()
        .limit(1)
        .findAll();

    final exercises = await _isar!.localExercises
        .where()
        .sortByLastSyncAttemptDesc()
        .limit(1)
        .findAll();

    final sets = await _isar!.localExerciseSets
        .where()
        .sortByLastSyncAttemptDesc()
        .limit(1)
        .findAll();

    final templates = await _isar!.localExerciseTemplates
        .where()
        .sortByLastSyncAttemptDesc()
        .limit(1)
        .findAll();

    final times = <DateTime?>[
      sessions.isNotEmpty ? sessions.first.lastSyncAttempt : null,
      exercises.isNotEmpty ? exercises.first.lastSyncAttempt : null,
      sets.isNotEmpty ? sets.first.lastSyncAttempt : null,
      templates.isNotEmpty ? templates.first.lastSyncAttempt : null,
    ].whereType<DateTime>();

    if (times.isEmpty) return null;

    return times.reduce((a, b) => a.isAfter(b) ? a : b);
  }
}
