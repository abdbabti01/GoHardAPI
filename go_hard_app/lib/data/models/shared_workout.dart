import 'package:isar/isar.dart';

part 'shared_workout.g.dart';

/// Represents a shared workout in the community
@collection
class SharedWorkout {
  Id id = Isar.autoIncrement;

  /// Original session/template ID
  late int originalId;

  /// Type: 'session' or 'template'
  late String type;

  /// User who shared it
  @Index()
  late int sharedByUserId;

  late String sharedByUserName;

  /// Workout details
  late String workoutName;
  String? description;
  late String exercisesJson;

  /// Stats
  late int duration; // minutes
  late String category;
  String? difficulty;

  /// Social metrics
  late int likeCount;
  late int saveCount;
  late int commentCount;

  /// Has current user liked/saved this?
  late bool isLikedByCurrentUser;
  late bool isSavedByCurrentUser;

  late DateTime sharedAt;

  SharedWorkout({
    this.id = Isar.autoIncrement,
    required this.originalId,
    required this.type,
    required this.sharedByUserId,
    required this.sharedByUserName,
    required this.workoutName,
    this.description,
    required this.exercisesJson,
    required this.duration,
    required this.category,
    this.difficulty,
    this.likeCount = 0,
    this.saveCount = 0,
    this.commentCount = 0,
    this.isLikedByCurrentUser = false,
    this.isSavedByCurrentUser = false,
    required this.sharedAt,
  });

  /// Format duration
  String get formattedDuration {
    if (duration < 60) return '${duration}m';
    final hours = duration ~/ 60;
    final mins = duration % 60;
    return mins > 0 ? '${hours}h ${mins}m' : '${hours}h';
  }

  /// Get time since shared
  String get timeSinceShared {
    final now = DateTime.now();
    final difference = now.difference(sharedAt);

    if (difference.inDays > 30) {
      final months = difference.inDays ~/ 30;
      return '$months month${months > 1 ? 's' : ''} ago';
    } else if (difference.inDays > 0) {
      return '${difference.inDays} day${difference.inDays > 1 ? 's' : ''} ago';
    } else if (difference.inHours > 0) {
      return '${difference.inHours} hour${difference.inHours > 1 ? 's' : ''} ago';
    } else if (difference.inMinutes > 0) {
      return '${difference.inMinutes} minute${difference.inMinutes > 1 ? 's' : ''} ago';
    } else {
      return 'Just now';
    }
  }

  SharedWorkout copyWith({
    int? id,
    int? originalId,
    String? type,
    int? sharedByUserId,
    String? sharedByUserName,
    String? workoutName,
    String? description,
    String? exercisesJson,
    int? duration,
    String? category,
    String? difficulty,
    int? likeCount,
    int? saveCount,
    int? commentCount,
    bool? isLikedByCurrentUser,
    bool? isSavedByCurrentUser,
    DateTime? sharedAt,
  }) {
    return SharedWorkout(
      id: id ?? this.id,
      originalId: originalId ?? this.originalId,
      type: type ?? this.type,
      sharedByUserId: sharedByUserId ?? this.sharedByUserId,
      sharedByUserName: sharedByUserName ?? this.sharedByUserName,
      workoutName: workoutName ?? this.workoutName,
      description: description ?? this.description,
      exercisesJson: exercisesJson ?? this.exercisesJson,
      duration: duration ?? this.duration,
      category: category ?? this.category,
      difficulty: difficulty ?? this.difficulty,
      likeCount: likeCount ?? this.likeCount,
      saveCount: saveCount ?? this.saveCount,
      commentCount: commentCount ?? this.commentCount,
      isLikedByCurrentUser: isLikedByCurrentUser ?? this.isLikedByCurrentUser,
      isSavedByCurrentUser: isSavedByCurrentUser ?? this.isSavedByCurrentUser,
      sharedAt: sharedAt ?? this.sharedAt,
    );
  }
}
