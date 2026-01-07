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
  final String? name;
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
    this.name,
    this.status = 'draft',
    this.startedAt,
    this.completedAt,
    this.pausedAt,
    this.exercises = const [],
  });

  // Helper method to mark datetime as UTC without conversion
  // Database timestamps are already stored as UTC, just need to mark them
  static DateTime? _toUtc(DateTime? dt) {
    if (dt == null) return null;
    // If already marked as UTC, return as-is
    if (dt.isUtc) return dt;
    // CRITICAL: Don't convert! Just mark as UTC since DB stores UTC values
    // Using toUtc() would double-convert and add timezone offset
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

    // Convert timestamp fields to UTC using proper conversion
    // NOTE: Do NOT convert the 'date' field - it's date-only and should stay in local timezone
    // Timestamps (startedAt, completedAt, pausedAt) MUST be UTC for proper time calculations
    // Using toUtc() instead of reinterpretation to handle JSON parser timezone conversions
    return Session(
      id: session.id,
      userId: session.userId,
      date: session.date, // Keep in local timezone (date-only field)
      duration: session.duration,
      notes: session.notes,
      type: session.type,
      name: session.name, // Preserve workout name
      status: session.status,
      startedAt: _toUtc(session.startedAt), // Convert to UTC (timestamp)
      completedAt: _toUtc(session.completedAt), // Convert to UTC (timestamp)
      pausedAt: _toUtc(session.pausedAt), // Convert to UTC (timestamp)
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
    String? name,
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
      name: name ?? this.name,
      status: status ?? this.status,
      startedAt: startedAt ?? this.startedAt,
      completedAt: completedAt ?? this.completedAt,
      pausedAt: pausedAt ?? this.pausedAt,
      exercises: exercises ?? this.exercises,
    );
  }
}
