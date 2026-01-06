/// Represents an AI-generated workout suggestion
class WorkoutSuggestion {
  final String id;
  final String title;
  final String description;
  final List<String> exercises;
  final int estimatedDuration; // minutes
  final String difficulty; // 'beginner', 'intermediate', 'advanced'
  final String category; // 'strength', 'cardio', 'flexibility', etc.
  final double matchScore; // 0-1, how well it matches user's goals
  final String reason; // Why this is suggested
  final DateTime suggestedAt;

  const WorkoutSuggestion({
    required this.id,
    required this.title,
    required this.description,
    required this.exercises,
    required this.estimatedDuration,
    required this.difficulty,
    required this.category,
    required this.matchScore,
    required this.reason,
    required this.suggestedAt,
  });

  /// Get difficulty color
  String get difficultyColor {
    switch (difficulty.toLowerCase()) {
      case 'beginner':
        return 'green';
      case 'intermediate':
        return 'orange';
      case 'advanced':
        return 'red';
      default:
        return 'blue';
    }
  }

  /// Get difficulty icon
  String get difficultyIcon {
    switch (difficulty.toLowerCase()) {
      case 'beginner':
        return '🌱';
      case 'intermediate':
        return '💪';
      case 'advanced':
        return '🔥';
      default:
        return '⭐';
    }
  }

  /// Format match score as percentage
  String get matchPercentage => '${(matchScore * 100).round()}%';

  factory WorkoutSuggestion.fromJson(Map<String, dynamic> json) {
    return WorkoutSuggestion(
      id: json['id'] as String,
      title: json['title'] as String,
      description: json['description'] as String,
      exercises: (json['exercises'] as List).cast<String>(),
      estimatedDuration: json['estimatedDuration'] as int,
      difficulty: json['difficulty'] as String,
      category: json['category'] as String,
      matchScore: (json['matchScore'] as num).toDouble(),
      reason: json['reason'] as String,
      suggestedAt: DateTime.parse(json['suggestedAt'] as String),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'title': title,
      'description': description,
      'exercises': exercises,
      'estimatedDuration': estimatedDuration,
      'difficulty': difficulty,
      'category': category,
      'matchScore': matchScore,
      'reason': reason,
      'suggestedAt': suggestedAt.toIso8601String(),
    };
  }
}
