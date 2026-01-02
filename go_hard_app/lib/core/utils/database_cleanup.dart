import 'package:flutter/foundation.dart';
import 'package:isar/isar.dart';
import '../../data/local/models/local_session.dart';
import '../../data/local/models/local_exercise.dart';
import '../../data/local/models/local_exercise_set.dart';

/// Utility class for cleaning up corrupted database records
class DatabaseCleanup {
  /// Delete all sessions that have failed sync attempts
  /// and don't exist on the server (404 errors)
  static Future<void> cleanupFailedSessions(Isar db) async {
    debugPrint('🧹 Starting database cleanup...');

    try {
      // Get all sessions with sync errors (retry count >= 3)
      final failedSessions =
          await db.localSessions
              .filter()
              .syncRetryCountGreaterThan(2)
              .findAll();

      if (failedSessions.isEmpty) {
        debugPrint('✅ No failed sessions to clean up');
        return;
      }

      debugPrint(
        '🗑️ Found ${failedSessions.length} failed sessions to delete',
      );

      // Delete each failed session and its exercises
      for (final session in failedSessions) {
        await db.writeTxn(() async {
          // Delete exercises for this session
          final exercises =
              await db.localExercises
                  .filter()
                  .sessionLocalIdEqualTo(session.localId)
                  .findAll();

          for (final exercise in exercises) {
            // Delete sets for this exercise
            await db.localExerciseSets
                .filter()
                .exerciseLocalIdEqualTo(exercise.localId)
                .deleteAll();
          }

          // Delete exercises
          await db.localExercises
              .filter()
              .sessionLocalIdEqualTo(session.localId)
              .deleteAll();

          // Delete session
          await db.localSessions.delete(session.localId);

          debugPrint(
            '  🗑️ Deleted session ${session.serverId ?? session.localId} with ${exercises.length} exercises',
          );
        });
      }

      debugPrint('✅ Cleanup completed');
    } catch (e) {
      debugPrint('❌ Cleanup failed: $e');
    }
  }

  /// Delete ALL local data (nuclear option for fresh start)
  static Future<void> clearAllData(Isar db) async {
    debugPrint('🧹 Clearing ALL local data...');

    try {
      await db.writeTxn(() async {
        await db.localExerciseSets.clear();
        await db.localExercises.clear();
        await db.localSessions.clear();
        // Don't clear exercise templates as they're static data
      });

      debugPrint('✅ All data cleared');
    } catch (e) {
      debugPrint('❌ Clear failed: $e');
    }
  }
}
