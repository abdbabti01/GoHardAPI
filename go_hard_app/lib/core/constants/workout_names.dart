/// Predefined workout name categories
class WorkoutNames {
  // Strength / Muscle Split Days
  static const String chestDay = 'Chest Day';
  static const String backDay = 'Back Day';
  static const String shoulderDay = 'Shoulder Day';
  static const String armDay = 'Arm Day';
  static const String legDay = 'Leg Day';
  static const String glutesDay = 'Glutes Day';
  static const String absDay = 'Abs / Core Day';
  static const String fullBodyDay = 'Full Body Day';

  // Push / Pull / Legs
  static const String pushDay = 'Push Day';
  static const String pullDay = 'Pull Day';
  static const String pplLegsDay = 'Legs Day';

  // Performance & Conditioning
  static const String hiitDay = 'HIIT Day';
  static const String cardioDay = 'Cardio Day';
  static const String enduranceDay = 'Endurance Day';
  static const String conditioningDay = 'Conditioning Day';
  static const String athleticDay = 'Athletic Performance Day';

  /// Grouped workout names for display
  static const Map<String, List<String>> groupedWorkoutNames = {
    'Strength / Muscle Split Days': [
      chestDay,
      backDay,
      shoulderDay,
      armDay,
      legDay,
      glutesDay,
      absDay,
      fullBodyDay,
    ],
    '🔥 Push / Pull / Legs (Very Popular)': [pushDay, pullDay, pplLegsDay],
    '💥 Performance & Conditioning': [
      hiitDay,
      cardioDay,
      enduranceDay,
      conditioningDay,
      athleticDay,
    ],
  };

  /// All workout names as a flat list
  static List<String> get allWorkoutNames {
    return groupedWorkoutNames.values.expand((names) => names).toList();
  }
}
