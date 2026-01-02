import 'package:flutter_test/flutter_test.dart';
import 'package:go_hard_app/data/models/session.dart';
import 'package:go_hard_app/data/models/exercise.dart';
import 'package:go_hard_app/data/models/exercise_set.dart';
import 'package:go_hard_app/data/models/exercise_template.dart';
import 'package:go_hard_app/data/local/models/local_session.dart';
import 'package:go_hard_app/data/local/models/local_exercise.dart';
import 'package:go_hard_app/data/local/models/local_exercise_set.dart';
import 'package:go_hard_app/data/local/models/local_exercise_template.dart';
import 'package:go_hard_app/data/local/services/model_mapper.dart';

void main() {
  group('ModelMapper - Session Conversion', () {
    test('sessionToLocal should convert API Session to LocalSession', () {
      // Arrange
      final apiSession = Session(
        id: 123,
        userId: 456,
        date: DateTime(2024, 1, 15),
        duration: 3600,
        notes: 'Test workout',
        type: 'strength',
        status: 'completed',
        startedAt: DateTime(2024, 1, 15, 10, 0),
        completedAt: DateTime(2024, 1, 15, 11, 0),
        pausedAt: null,
        exercises: [],
      );

      // Act
      final localSession = ModelMapper.sessionToLocal(apiSession);

      // Assert
      expect(localSession.serverId, 123);
      expect(localSession.userId, 456);
      expect(localSession.date, DateTime(2024, 1, 15));
      expect(localSession.duration, 3600);
      expect(localSession.notes, 'Test workout');
      expect(localSession.type, 'strength');
      expect(localSession.status, 'completed');
      expect(localSession.isSynced, true);
      expect(localSession.syncStatus, 'synced');
      expect(localSession.startedAt, DateTime(2024, 1, 15, 10, 0));
      expect(localSession.completedAt, DateTime(2024, 1, 15, 11, 0));
      expect(localSession.pausedAt, null);
    });

    test('sessionToLocal should mark as unsynced when specified', () {
      // Arrange
      final apiSession = Session(
        id: 123,
        userId: 456,
        date: DateTime(2024, 1, 15),
        duration: 3600,
        notes: 'Test workout',
        type: 'strength',
        status: 'in_progress',
        exercises: [],
      );

      // Act
      final localSession = ModelMapper.sessionToLocal(
        apiSession,
        isSynced: false,
      );

      // Assert
      expect(localSession.serverId, 123);
      expect(localSession.isSynced, false);
      expect(localSession.syncStatus, 'pending_create');
    });

    test('localToSession should convert LocalSession to API Session', () {
      // Arrange
      final localSession = LocalSession(
        serverId: 123,
        userId: 456,
        date: DateTime(2024, 1, 15),
        duration: 3600,
        notes: 'Test workout',
        type: 'strength',
        status: 'completed',
        startedAt: DateTime(2024, 1, 15, 10, 0),
        completedAt: DateTime(2024, 1, 15, 11, 0),
        pausedAt: null,
        isSynced: true,
        syncStatus: 'synced',
        lastModifiedLocal: DateTime.now(),
      );

      // Act
      final apiSession = ModelMapper.localToSession(localSession);

      // Assert
      expect(apiSession.id, 123);
      expect(apiSession.userId, 456);
      expect(apiSession.date, DateTime(2024, 1, 15));
      expect(apiSession.duration, 3600);
      expect(apiSession.notes, 'Test workout');
      expect(apiSession.type, 'strength');
      expect(apiSession.status, 'completed');
      expect(apiSession.startedAt, DateTime(2024, 1, 15, 10, 0));
      expect(apiSession.completedAt, DateTime(2024, 1, 15, 11, 0));
      expect(apiSession.pausedAt, null);
    });

    test(
      'localToSession should use 0 for unsynced sessions without serverId',
      () {
        // Arrange
        final localSession = LocalSession(
          serverId: null, // Not synced yet
          userId: 456,
          date: DateTime(2024, 1, 15),
          duration: 3600,
          notes: 'Test workout',
          type: 'strength',
          status: 'in_progress',
          isSynced: false,
          syncStatus: 'pending_create',
          lastModifiedLocal: DateTime.now(),
        )..localId = 0; // Explicitly set localId for testing

        // Act
        final apiSession = ModelMapper.localToSession(localSession);

        // Assert
        expect(apiSession.id, 0); // Should use localId when serverId is null
        expect(apiSession.userId, 456);
        expect(apiSession.status, 'in_progress');
      },
    );

    test('sessionToLocal and localToSession should be bidirectional', () {
      // Arrange
      final originalSession = Session(
        id: 999,
        userId: 111,
        date: DateTime(2024, 1, 15),
        duration: 7200,
        notes: 'Bidirectional test',
        type: 'cardio',
        status: 'completed',
        exercises: [],
      );

      // Act - Convert to local and back to API
      final localSession = ModelMapper.sessionToLocal(originalSession);
      final convertedSession = ModelMapper.localToSession(localSession);

      // Assert - Should match original
      expect(convertedSession.id, originalSession.id);
      expect(convertedSession.userId, originalSession.userId);
      expect(convertedSession.date, originalSession.date);
      expect(convertedSession.duration, originalSession.duration);
      expect(convertedSession.notes, originalSession.notes);
      expect(convertedSession.type, originalSession.type);
      expect(convertedSession.status, originalSession.status);
    });
  });

  group('ModelMapper - Exercise Conversion', () {
    test('exerciseToLocal should convert API Exercise to LocalExercise', () {
      // Arrange
      final apiExercise = Exercise(
        id: 10,
        sessionId: 20,
        name: 'Bench Press',
        duration: 300,
        restTime: 90,
        notes: 'Heavy day',
        exerciseTemplateId: 5,
        exerciseSets: [],
      );

      // Act
      final localExercise = ModelMapper.exerciseToLocal(
        apiExercise,
        sessionLocalId: 100,
        sessionServerId: 20,
      );

      // Assert
      expect(localExercise.serverId, 10);
      expect(localExercise.sessionLocalId, 100);
      expect(localExercise.sessionServerId, 20);
      expect(localExercise.name, 'Bench Press');
      expect(localExercise.duration, 300);
      expect(localExercise.restTime, 90);
      expect(localExercise.notes, 'Heavy day');
      expect(localExercise.exerciseTemplateId, 5);
      expect(localExercise.isSynced, true);
      expect(localExercise.syncStatus, 'synced');
    });

    test('localToExercise should convert LocalExercise to API Exercise', () {
      // Arrange
      final localExercise = LocalExercise(
        serverId: 10,
        sessionLocalId: 100,
        sessionServerId: 20,
        name: 'Bench Press',
        duration: 300,
        restTime: 90,
        notes: 'Heavy day',
        exerciseTemplateId: 5,
        isSynced: true,
        syncStatus: 'synced',
        lastModifiedLocal: DateTime.now(),
      );

      // Act
      final apiExercise = ModelMapper.localToExercise(localExercise);

      // Assert
      expect(apiExercise.id, 10);
      expect(apiExercise.sessionId, 20);
      expect(apiExercise.name, 'Bench Press');
      expect(apiExercise.duration, 300);
      expect(apiExercise.restTime, 90);
      expect(apiExercise.notes, 'Heavy day');
      expect(apiExercise.exerciseTemplateId, 5);
    });
  });

  group('ModelMapper - ExerciseSet Conversion', () {
    test(
      'exerciseSetToLocal should convert API ExerciseSet to LocalExerciseSet',
      () {
        // Arrange
        final apiSet = ExerciseSet(
          id: 1,
          exerciseId: 10,
          setNumber: 1,
          reps: 10,
          weight: 135.0,
          duration: 30,
          isCompleted: true,
          completedAt: DateTime(2024, 1, 15, 10, 30),
          notes: 'Good form',
        );

        // Act
        final localSet = ModelMapper.exerciseSetToLocal(
          apiSet,
          exerciseLocalId: 50,
          exerciseServerId: 10,
        );

        // Assert
        expect(localSet.serverId, 1);
        expect(localSet.exerciseLocalId, 50);
        expect(localSet.exerciseServerId, 10);
        expect(localSet.setNumber, 1);
        expect(localSet.reps, 10);
        expect(localSet.weight, 135.0);
        expect(localSet.duration, 30);
        expect(localSet.isCompleted, true);
        expect(localSet.completedAt, DateTime(2024, 1, 15, 10, 30));
        expect(localSet.notes, 'Good form');
        expect(localSet.isSynced, true);
        expect(localSet.syncStatus, 'synced');
      },
    );

    test(
      'localToExerciseSet should convert LocalExerciseSet to API ExerciseSet',
      () {
        // Arrange
        final localSet = LocalExerciseSet(
          serverId: 1,
          exerciseLocalId: 50,
          exerciseServerId: 10,
          setNumber: 2,
          reps: 8,
          weight: 140.0,
          duration: 35,
          isCompleted: false,
          completedAt: null,
          notes: null,
          isSynced: true,
          syncStatus: 'synced',
          lastModifiedLocal: DateTime.now(),
        );

        // Act
        final apiSet = ModelMapper.localToExerciseSet(localSet);

        // Assert
        expect(apiSet.id, 1);
        expect(apiSet.exerciseId, 10);
        expect(apiSet.setNumber, 2);
        expect(apiSet.reps, 8);
        expect(apiSet.weight, 140.0);
        expect(apiSet.duration, 35);
        expect(apiSet.isCompleted, false);
        expect(apiSet.completedAt, null);
        expect(apiSet.notes, null);
      },
    );
  });

  group('ModelMapper - ExerciseTemplate Conversion', () {
    test('exerciseTemplateToLocal should convert API ExerciseTemplate', () {
      // Arrange
      final apiTemplate = ExerciseTemplate(
        id: 5,
        name: 'Bench Press',
        description: 'Classic chest exercise',
        category: 'Strength',
        muscleGroup: 'Chest',
        equipment: 'Barbell',
        difficulty: 'Intermediate',
        videoUrl: 'https://example.com/video',
        imageUrl: 'https://example.com/image',
        instructions: '1. Lie on bench\n2. Lower bar\n3. Press up',
        isCustom: false,
        createdByUserId: null,
      );

      // Act
      final localTemplate = ModelMapper.exerciseTemplateToLocal(apiTemplate);

      // Assert
      expect(localTemplate.serverId, 5);
      expect(localTemplate.name, 'Bench Press');
      expect(localTemplate.description, 'Classic chest exercise');
      expect(localTemplate.category, 'Strength');
      expect(localTemplate.muscleGroup, 'Chest');
      expect(localTemplate.equipment, 'Barbell');
      expect(localTemplate.difficulty, 'Intermediate');
      expect(localTemplate.videoUrl, 'https://example.com/video');
      expect(localTemplate.imageUrl, 'https://example.com/image');
      expect(
        localTemplate.instructions,
        '1. Lie on bench\n2. Lower bar\n3. Press up',
      );
      expect(localTemplate.isCustom, false);
      expect(localTemplate.createdByUserId, null);
      expect(localTemplate.isSynced, true);
      expect(localTemplate.syncStatus, 'synced');
    });

    test('localToExerciseTemplate should convert LocalExerciseTemplate', () {
      // Arrange
      final localTemplate = LocalExerciseTemplate(
        serverId: 5,
        name: 'Squat',
        description: 'Leg exercise',
        category: 'Strength',
        muscleGroup: 'Legs',
        equipment: 'Barbell',
        difficulty: 'Advanced',
        videoUrl: null,
        imageUrl: null,
        instructions: 'Squat down',
        isCustom: true,
        createdByUserId: 123,
        isSynced: true,
        syncStatus: 'synced',
        lastModifiedLocal: DateTime.now(),
      );

      // Act
      final apiTemplate = ModelMapper.localToExerciseTemplate(localTemplate);

      // Assert
      expect(apiTemplate.id, 5);
      expect(apiTemplate.name, 'Squat');
      expect(apiTemplate.description, 'Leg exercise');
      expect(apiTemplate.category, 'Strength');
      expect(apiTemplate.muscleGroup, 'Legs');
      expect(apiTemplate.equipment, 'Barbell');
      expect(apiTemplate.difficulty, 'Advanced');
      expect(apiTemplate.isCustom, true);
      expect(apiTemplate.createdByUserId, 123);
    });
  });

  group('ModelMapper - Batch Conversions', () {
    test('sessionsToLocal should convert list of sessions', () {
      // Arrange
      final apiSessions = [
        Session(id: 1, userId: 100, date: DateTime(2024, 1, 1), exercises: []),
        Session(id: 2, userId: 100, date: DateTime(2024, 1, 2), exercises: []),
        Session(id: 3, userId: 100, date: DateTime(2024, 1, 3), exercises: []),
      ];

      // Act
      final localSessions = ModelMapper.sessionsToLocal(apiSessions);

      // Assert
      expect(localSessions.length, 3);
      expect(localSessions[0].serverId, 1);
      expect(localSessions[1].serverId, 2);
      expect(localSessions[2].serverId, 3);
      expect(localSessions.every((s) => s.isSynced), true);
    });

    test('localToSessions should convert list of local sessions', () {
      // Arrange
      final localSessions = [
        LocalSession(
          serverId: 1,
          userId: 100,
          date: DateTime(2024, 1, 1),
          isSynced: true,
          syncStatus: 'synced',
          lastModifiedLocal: DateTime.now(),
        ),
        LocalSession(
          serverId: 2,
          userId: 100,
          date: DateTime(2024, 1, 2),
          isSynced: true,
          syncStatus: 'synced',
          lastModifiedLocal: DateTime.now(),
        ),
      ];

      // Act
      final apiSessions = ModelMapper.localToSessions(localSessions);

      // Assert
      expect(apiSessions.length, 2);
      expect(apiSessions[0].id, 1);
      expect(apiSessions[1].id, 2);
    });
  });
}
