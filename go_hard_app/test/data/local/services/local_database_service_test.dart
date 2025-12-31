import 'package:flutter_test/flutter_test.dart';
import 'package:go_hard_app/data/local/services/local_database_service.dart';
import 'package:go_hard_app/data/local/models/local_session.dart';
import 'package:go_hard_app/data/local/models/local_exercise.dart';
import 'package:go_hard_app/data/local/models/local_exercise_set.dart';
import 'package:go_hard_app/data/local/models/local_exercise_template.dart';

void main() {
  // Initialize Flutter test bindings before any tests run
  TestWidgetsFlutterBinding.ensureInitialized();

  group('LocalDatabaseService', () {
    late LocalDatabaseService dbService;

    setUp(() {
      dbService = LocalDatabaseService.instance;
    });

    tearDown(() async {
      if (dbService.isInitialized) {
        await dbService.clearAll();
        await dbService.close();
      }
    });

    test('should initialize database successfully', () async {
      final db = await dbService.initialize();

      expect(db, isNotNull);
      expect(dbService.isInitialized, isTrue);
      expect(db.isOpen, isTrue);
    });

    test('should return same instance when initialized multiple times', () async {
      final db1 = await dbService.initialize();
      final db2 = await dbService.initialize();

      expect(db1, equals(db2));
    });

    test('should register all schemas correctly', () async {
      final db = await dbService.initialize();

      // Verify all collections are available
      expect(db.localSessions, isNotNull);
      expect(db.localExercises, isNotNull);
      expect(db.localExerciseSets, isNotNull);
      expect(db.localExerciseTemplates, isNotNull);
    });

    test('should create and read LocalSession', () async {
      final db = await dbService.initialize();

      final session = LocalSession(
        userId: 1,
        date: DateTime.now(),
        status: 'draft',
        lastModifiedLocal: DateTime.now(),
      );

      await db.writeTxn(() async {
        await db.localSessions.put(session);
      });

      final savedSession = await db.localSessions.get(session.localId);

      expect(savedSession, isNotNull);
      expect(savedSession!.userId, equals(1));
      expect(savedSession.status, equals('draft'));
      expect(savedSession.syncStatus, equals('pending_create'));
    });

    test('should create and read LocalExercise', () async {
      final db = await dbService.initialize();

      final exercise = LocalExercise(
        sessionLocalId: 1,
        name: 'Bench Press',
        lastModifiedLocal: DateTime.now(),
      );

      await db.writeTxn(() async {
        await db.localExercises.put(exercise);
      });

      final savedExercise = await db.localExercises.get(exercise.localId);

      expect(savedExercise, isNotNull);
      expect(savedExercise!.name, equals('Bench Press'));
      expect(savedExercise.syncStatus, equals('pending_create'));
    });

    test('should create and read LocalExerciseSet', () async {
      final db = await dbService.initialize();

      final set = LocalExerciseSet(
        exerciseLocalId: 1,
        setNumber: 1,
        reps: 10,
        weight: 100.0,
        lastModifiedLocal: DateTime.now(),
      );

      await db.writeTxn(() async {
        await db.localExerciseSets.put(set);
      });

      final savedSet = await db.localExerciseSets.get(set.localId);

      expect(savedSet, isNotNull);
      expect(savedSet!.reps, equals(10));
      expect(savedSet.weight, equals(100.0));
      expect(savedSet.syncStatus, equals('pending_create'));
    });

    test('should create and read LocalExerciseTemplate', () async {
      final db = await dbService.initialize();

      final template = LocalExerciseTemplate(
        name: 'Squat',
        category: 'strength',
        muscleGroup: 'legs',
        lastModifiedLocal: DateTime.now(),
      );

      await db.writeTxn(() async {
        await db.localExerciseTemplates.put(template);
      });

      final savedTemplate = await db.localExerciseTemplates.get(template.localId);

      expect(savedTemplate, isNotNull);
      expect(savedTemplate!.name, equals('Squat'));
      expect(savedTemplate.category, equals('strength'));
      expect(savedTemplate.muscleGroup, equals('legs'));
    });

    test('should count pending sync items correctly', () async {
      final db = await dbService.initialize();

      // Create some unsynced items
      final session = LocalSession(
        userId: 1,
        date: DateTime.now(),
        status: 'draft',
        isSynced: false,
        lastModifiedLocal: DateTime.now(),
      );

      final exercise = LocalExercise(
        sessionLocalId: 1,
        name: 'Test Exercise',
        isSynced: false,
        lastModifiedLocal: DateTime.now(),
      );

      await db.writeTxn(() async {
        await db.localSessions.put(session);
        await db.localExercises.put(exercise);
      });

      final count = await dbService.getPendingSyncCount();

      expect(count, equals(2));
    });

    test('should clear all data', () async {
      final db = await dbService.initialize();

      // Add some data
      final session = LocalSession(
        userId: 1,
        date: DateTime.now(),
        status: 'draft',
        lastModifiedLocal: DateTime.now(),
      );

      await db.writeTxn(() async {
        await db.localSessions.put(session);
      });

      // Verify data exists
      var count = await db.localSessions.count();
      expect(count, equals(1));

      // Clear all data
      await dbService.clearAll();

      // Verify data is gone
      count = await db.localSessions.count();
      expect(count, equals(0));
    });

    test('should throw error when accessing database before initialization', () {
      final uninitializedService = LocalDatabaseService.instance;

      if (uninitializedService.isInitialized) {
        // Skip this test if database is already initialized from previous tests
        return;
      }

      expect(
        () => uninitializedService.database,
        throwsA(isA<Exception>()),
      );
    });
  });
}
