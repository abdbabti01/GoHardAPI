import '../../models/session.dart';
import '../../models/exercise.dart';
import '../../models/exercise_set.dart';
import '../../models/exercise_template.dart';
import '../models/local_session.dart';
import '../models/local_exercise.dart';
import '../models/local_exercise_set.dart';
import '../models/local_exercise_template.dart';

/// Service for converting between API models and local database models
class ModelMapper {
  // ========== Session Mapping ==========

  /// Convert API Session to LocalSession
  /// Used when caching data from server
  static LocalSession sessionToLocal(
    Session apiSession, {
    int? localId,
    bool isSynced = true,
  }) {
    final session = LocalSession(
      serverId: apiSession.id,
      userId: apiSession.userId,
      date: apiSession.date,
      duration: apiSession.duration,
      notes: apiSession.notes,
      type: apiSession.type,
      name: apiSession.name,
      status: apiSession.status,
      startedAt: apiSession.startedAt,
      completedAt: apiSession.completedAt,
      pausedAt: apiSession.pausedAt,
      isSynced: isSynced,
      syncStatus: isSynced ? 'synced' : 'pending_create',
      lastModifiedLocal: DateTime.now(),
      lastModifiedServer: DateTime.now(),
    );

    // Preserve existing localId if updating an existing session
    if (localId != null) {
      session.localId = localId;
    }

    return session;
  }

  /// Convert LocalSession to API Session
  /// Used when sending local data to server or displaying in UI
  static Session localToSession(
    LocalSession localSession, {
    List<Exercise> exercises = const [],
  }) {
    return Session(
      id:
          localSession.serverId ??
          localSession.localId, // Use localId for unsynced items
      userId: localSession.userId,
      date: localSession.date,
      duration: localSession.duration,
      notes: localSession.notes,
      type: localSession.type,
      name: localSession.name,
      status: localSession.status,
      startedAt: localSession.startedAt,
      completedAt: localSession.completedAt,
      pausedAt: localSession.pausedAt,
      exercises: exercises,
    );
  }

  // ========== Exercise Mapping ==========

  /// Convert API Exercise to LocalExercise
  /// Requires localSessionId for parent reference
  static LocalExercise exerciseToLocal(
    Exercise apiExercise, {
    required int sessionLocalId,
    int? sessionServerId,
    int? localId,
    bool isSynced = true,
  }) {
    final exercise = LocalExercise(
      serverId: apiExercise.id,
      sessionLocalId: sessionLocalId,
      sessionServerId: sessionServerId ?? apiExercise.sessionId,
      name: apiExercise.name,
      duration: apiExercise.duration,
      restTime: apiExercise.restTime,
      notes: apiExercise.notes,
      exerciseTemplateId: apiExercise.exerciseTemplateId,
      isSynced: isSynced,
      syncStatus: isSynced ? 'synced' : 'pending_create',
      lastModifiedLocal: DateTime.now(),
      lastModifiedServer: DateTime.now(),
    );

    // Preserve existing localId if updating an existing exercise
    if (localId != null) {
      exercise.localId = localId;
    }

    return exercise;
  }

  /// Convert LocalExercise to API Exercise
  static Exercise localToExercise(
    LocalExercise localExercise, {
    List<ExerciseSet> exerciseSets = const [],
  }) {
    return Exercise(
      id: localExercise.serverId ?? localExercise.localId,
      sessionId: localExercise.sessionServerId ?? 0,
      name: localExercise.name,
      duration: localExercise.duration,
      restTime: localExercise.restTime,
      notes: localExercise.notes,
      exerciseTemplateId: localExercise.exerciseTemplateId,
      exerciseSets: exerciseSets,
    );
  }

  // ========== ExerciseSet Mapping ==========

  /// Convert API ExerciseSet to LocalExerciseSet
  /// Requires exerciseLocalId for parent reference
  static LocalExerciseSet exerciseSetToLocal(
    ExerciseSet apiSet, {
    required int exerciseLocalId,
    int? exerciseServerId,
    int? localId,
    bool isSynced = true,
  }) {
    final set = LocalExerciseSet(
      serverId: apiSet.id,
      exerciseLocalId: exerciseLocalId,
      exerciseServerId: exerciseServerId ?? apiSet.exerciseId,
      setNumber: apiSet.setNumber,
      reps: apiSet.reps,
      weight: apiSet.weight,
      duration: apiSet.duration,
      isCompleted: apiSet.isCompleted,
      completedAt: apiSet.completedAt,
      notes: apiSet.notes,
      isSynced: isSynced,
      syncStatus: isSynced ? 'synced' : 'pending_create',
      lastModifiedLocal: DateTime.now(),
      lastModifiedServer: DateTime.now(),
    );

    // Preserve existing localId if updating an existing set
    if (localId != null) {
      set.localId = localId;
    }

    return set;
  }

  /// Convert LocalExerciseSet to API ExerciseSet
  static ExerciseSet localToExerciseSet(LocalExerciseSet localSet) {
    return ExerciseSet(
      id: localSet.serverId ?? 0,
      exerciseId: localSet.exerciseServerId ?? 0,
      setNumber: localSet.setNumber,
      reps: localSet.reps,
      weight: localSet.weight,
      duration: localSet.duration,
      isCompleted: localSet.isCompleted,
      completedAt: localSet.completedAt,
      notes: localSet.notes,
    );
  }

  // ========== ExerciseTemplate Mapping ==========

  /// Convert API ExerciseTemplate to LocalExerciseTemplate
  static LocalExerciseTemplate exerciseTemplateToLocal(
    ExerciseTemplate apiTemplate, {
    int? localId,
    bool isSynced = true,
  }) {
    return LocalExerciseTemplate(
      serverId: apiTemplate.id,
      name: apiTemplate.name,
      description: apiTemplate.description,
      category: apiTemplate.category,
      muscleGroup: apiTemplate.muscleGroup,
      equipment: apiTemplate.equipment,
      difficulty: apiTemplate.difficulty,
      videoUrl: apiTemplate.videoUrl,
      imageUrl: apiTemplate.imageUrl,
      instructions: apiTemplate.instructions,
      isCustom: apiTemplate.isCustom,
      createdByUserId: apiTemplate.createdByUserId,
      isSynced: isSynced,
      syncStatus: isSynced ? 'synced' : 'pending_create',
      lastModifiedLocal: DateTime.now(),
      lastModifiedServer: DateTime.now(),
    );
  }

  /// Convert LocalExerciseTemplate to API ExerciseTemplate
  static ExerciseTemplate localToExerciseTemplate(
    LocalExerciseTemplate localTemplate,
  ) {
    return ExerciseTemplate(
      id: localTemplate.serverId ?? 0,
      name: localTemplate.name,
      description: localTemplate.description,
      category: localTemplate.category,
      muscleGroup: localTemplate.muscleGroup,
      equipment: localTemplate.equipment,
      difficulty: localTemplate.difficulty,
      videoUrl: localTemplate.videoUrl,
      imageUrl: localTemplate.imageUrl,
      instructions: localTemplate.instructions,
      isCustom: localTemplate.isCustom,
      createdByUserId: localTemplate.createdByUserId,
    );
  }

  // ========== Batch Mapping Helpers ==========

  /// Convert a list of API Sessions to LocalSessions
  static List<LocalSession> sessionsToLocal(
    List<Session> apiSessions, {
    bool isSynced = true,
  }) {
    return apiSessions
        .map((session) => sessionToLocal(session, isSynced: isSynced))
        .toList();
  }

  /// Convert a list of LocalSessions to API Sessions
  static List<Session> localToSessions(List<LocalSession> localSessions) {
    return localSessions.map((session) => localToSession(session)).toList();
  }

  /// Convert a list of API Exercises to LocalExercises
  static List<LocalExercise> exercisesToLocal(
    List<Exercise> apiExercises, {
    required int sessionLocalId,
    int? sessionServerId,
    bool isSynced = true,
  }) {
    return apiExercises
        .map(
          (exercise) => exerciseToLocal(
            exercise,
            sessionLocalId: sessionLocalId,
            sessionServerId: sessionServerId,
            isSynced: isSynced,
          ),
        )
        .toList();
  }

  /// Convert a list of LocalExercises to API Exercises
  static List<Exercise> localToExercises(List<LocalExercise> localExercises) {
    return localExercises.map((exercise) => localToExercise(exercise)).toList();
  }

  /// Convert a list of API ExerciseSets to LocalExerciseSets
  static List<LocalExerciseSet> exerciseSetsToLocal(
    List<ExerciseSet> apiSets, {
    required int exerciseLocalId,
    int? exerciseServerId,
    bool isSynced = true,
  }) {
    return apiSets
        .map(
          (set) => exerciseSetToLocal(
            set,
            exerciseLocalId: exerciseLocalId,
            exerciseServerId: exerciseServerId,
            isSynced: isSynced,
          ),
        )
        .toList();
  }

  /// Convert a list of LocalExerciseSets to API ExerciseSets
  static List<ExerciseSet> localToExerciseSets(
    List<LocalExerciseSet> localSets,
  ) {
    return localSets.map((set) => localToExerciseSet(set)).toList();
  }

  /// Convert a list of API ExerciseTemplates to LocalExerciseTemplates
  static List<LocalExerciseTemplate> exerciseTemplatesToLocal(
    List<ExerciseTemplate> apiTemplates, {
    bool isSynced = true,
  }) {
    return apiTemplates
        .map(
          (template) => exerciseTemplateToLocal(template, isSynced: isSynced),
        )
        .toList();
  }

  /// Convert a list of LocalExerciseTemplates to API ExerciseTemplates
  static List<ExerciseTemplate> localToExerciseTemplates(
    List<LocalExerciseTemplate> localTemplates,
  ) {
    return localTemplates
        .map((template) => localToExerciseTemplate(template))
        .toList();
  }
}
