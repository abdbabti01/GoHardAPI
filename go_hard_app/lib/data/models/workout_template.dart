import 'package:isar/isar.dart';

part 'workout_template.g.dart';

/// Workout template for creating recurring workout plans
@collection
class WorkoutTemplate {
  Id id = Isar.autoIncrement;

  @Index()
  late int userId;

  late String name;
  String? description;

  /// Template exercises (stored as JSON)
  late String exercisesJson;

  /// Recurrence pattern: 'weekly', 'daily', 'custom'
  late String recurrencePattern;

  /// Days of week for weekly pattern (1=Mon, 7=Sun), comma-separated: "1,3,5"
  String? daysOfWeek;

  /// Interval for custom pattern (e.g., every 2 days)
  int? intervalDays;

  /// Duration in minutes (estimated)
  int? estimatedDuration;

  /// Category/type of workout
  String? category;

  /// Is this template active?
  @Index()
  late bool isActive;

  /// Is this a community/shared template?
  late bool isCommunity;

  /// Original author user ID for community templates
  int? createdByUserId;

  /// Number of times this template has been used
  late int usageCount;

  /// Rating (1-5) for community templates
  double? rating;

  /// Number of ratings
  int? ratingCount;

  late DateTime createdAt;
  DateTime? updatedAt;

  WorkoutTemplate({
    this.id = Isar.autoIncrement,
    required this.userId,
    required this.name,
    this.description,
    required this.exercisesJson,
    required this.recurrencePattern,
    this.daysOfWeek,
    this.intervalDays,
    this.estimatedDuration,
    this.category,
    this.isActive = true,
    this.isCommunity = false,
    this.createdByUserId,
    this.usageCount = 0,
    this.rating,
    this.ratingCount,
    required this.createdAt,
    this.updatedAt,
  });

  /// Get days of week as list
  List<int> get daysOfWeekList {
    if (daysOfWeek == null || daysOfWeek!.isEmpty) return [];
    return daysOfWeek!.split(',').map((e) => int.parse(e.trim())).toList();
  }

  /// Set days of week from list
  set daysOfWeekList(List<int> days) {
    daysOfWeek = days.join(',');
  }

  /// Get next scheduled date based on recurrence pattern
  DateTime? getNextScheduledDate(DateTime fromDate) {
    switch (recurrencePattern) {
      case 'daily':
        return fromDate.add(const Duration(days: 1));

      case 'weekly':
        if (daysOfWeekList.isEmpty) return null;
        final currentWeekday = fromDate.weekday;

        // Find next day in the week
        for (var day in daysOfWeekList.where((d) => d > currentWeekday)) {
          return fromDate.add(Duration(days: day - currentWeekday));
        }

        // If no day found this week, get first day of next week
        final firstDay = daysOfWeekList.first;
        final daysUntilNextWeek = 7 - currentWeekday + firstDay;
        return fromDate.add(Duration(days: daysUntilNextWeek));

      case 'custom':
        if (intervalDays == null) return null;
        return fromDate.add(Duration(days: intervalDays!));

      default:
        return null;
    }
  }

  /// Format recurrence pattern for display
  String get recurrenceDisplay {
    switch (recurrencePattern) {
      case 'daily':
        return 'Every day';
      case 'weekly':
        if (daysOfWeekList.isEmpty) return 'Weekly';
        final dayNames = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
        final days = daysOfWeekList.map((d) => dayNames[d - 1]).join(', ');
        return 'Every $days';
      case 'custom':
        if (intervalDays == null) return 'Custom';
        return 'Every $intervalDays days';
      default:
        return recurrencePattern;
    }
  }

  /// Copy with method
  WorkoutTemplate copyWith({
    int? id,
    int? userId,
    String? name,
    String? description,
    String? exercisesJson,
    String? recurrencePattern,
    String? daysOfWeek,
    int? intervalDays,
    int? estimatedDuration,
    String? category,
    bool? isActive,
    bool? isCommunity,
    int? createdByUserId,
    int? usageCount,
    double? rating,
    int? ratingCount,
    DateTime? createdAt,
    DateTime? updatedAt,
  }) {
    return WorkoutTemplate(
      id: id ?? this.id,
      userId: userId ?? this.userId,
      name: name ?? this.name,
      description: description ?? this.description,
      exercisesJson: exercisesJson ?? this.exercisesJson,
      recurrencePattern: recurrencePattern ?? this.recurrencePattern,
      daysOfWeek: daysOfWeek ?? this.daysOfWeek,
      intervalDays: intervalDays ?? this.intervalDays,
      estimatedDuration: estimatedDuration ?? this.estimatedDuration,
      category: category ?? this.category,
      isActive: isActive ?? this.isActive,
      isCommunity: isCommunity ?? this.isCommunity,
      createdByUserId: createdByUserId ?? this.createdByUserId,
      usageCount: usageCount ?? this.usageCount,
      rating: rating ?? this.rating,
      ratingCount: ratingCount ?? this.ratingCount,
      createdAt: createdAt ?? this.createdAt,
      updatedAt: updatedAt ?? this.updatedAt,
    );
  }
}
