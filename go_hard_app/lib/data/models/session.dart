import 'package:json_annotation/json_annotation.dart';
import 'exercise.dart';

part 'session.g.dart';

@JsonSerializable()
class Session {
  final int id;
  final int userId;
  final DateTime date;
  final int? duration;
  final String? notes;
  final String? type;
  final String status;
  final DateTime? startedAt;
  final DateTime? completedAt;
  final DateTime? pausedAt;
  final List<Exercise> exercises;

  Session({
    required this.id,
    required this.userId,
    required this.date,
    this.duration,
    this.notes,
    this.type,
    this.status = 'draft',
    this.startedAt,
    this.completedAt,
    this.pausedAt,
    this.exercises = const [],
  });

  // Helper method to reinterpret datetime as UTC (not convert)
  static DateTime? _asUtc(DateTime? dt) {
    if (dt == null) return null;
    if (dt.isUtc) return dt;
    // Reinterpret the same values as UTC instead of converting
    return DateTime.utc(
      dt.year,
      dt.month,
      dt.day,
      dt.hour,
      dt.minute,
      dt.second,
      dt.millisecond,
      dt.microsecond,
    );
  }

  factory Session.fromJson(Map<String, dynamic> json) {
    // Parse the session using generated code
    final session = _$SessionFromJson(json);

    // Reinterpret datetimes as UTC (API sends UTC but JSON might parse as local)
    // NOTE: Do NOT convert the 'date' field - it's date-only and should stay in local timezone
    return Session(
      id: session.id,
      userId: session.userId,
      date: session.date,
      duration: session.duration,
      notes: session.notes,
      type: session.type,
      status: session.status,
      startedAt: _asUtc(session.startedAt),
      completedAt: _asUtc(session.completedAt),
      pausedAt: _asUtc(session.pausedAt),
      exercises: session.exercises,
    );
  }

  Map<String, dynamic> toJson() => _$SessionToJson(this);

  /// Create a copy of this Session with some fields replaced
  Session copyWith({
    int? id,
    int? userId,
    DateTime? date,
    int? duration,
    String? notes,
    String? type,
    String? status,
    DateTime? startedAt,
    DateTime? completedAt,
    DateTime? pausedAt,
    List<Exercise>? exercises,
  }) {
    return Session(
      id: id ?? this.id,
      userId: userId ?? this.userId,
      date: date ?? this.date,
      duration: duration ?? this.duration,
      notes: notes ?? this.notes,
      type: type ?? this.type,
      status: status ?? this.status,
      startedAt: startedAt ?? this.startedAt,
      completedAt: completedAt ?? this.completedAt,
      pausedAt: pausedAt ?? this.pausedAt,
      exercises: exercises ?? this.exercises,
    );
  }
}
