enum Gender {
  male,
  female,
  other,
  preferNotToSay;

  String get displayName {
    switch (this) {
      case Gender.male:
        return 'Male';
      case Gender.female:
        return 'Female';
      case Gender.other:
        return 'Other';
      case Gender.preferNotToSay:
        return 'Prefer not to say';
    }
  }

  String get serverValue {
    switch (this) {
      case Gender.male:
        return 'Male';
      case Gender.female:
        return 'Female';
      case Gender.other:
        return 'Other';
      case Gender.preferNotToSay:
        return 'PreferNotToSay';
    }
  }

  static Gender? fromString(String? value) {
    if (value == null) return null;
    switch (value) {
      case 'Male':
        return Gender.male;
      case 'Female':
        return Gender.female;
      case 'Other':
        return Gender.other;
      case 'PreferNotToSay':
        return Gender.preferNotToSay;
      default:
        return null;
    }
  }
}

enum ExperienceLevel {
  beginner,
  intermediate,
  advanced,
  expert;

  String get displayName {
    switch (this) {
      case ExperienceLevel.beginner:
        return 'Beginner';
      case ExperienceLevel.intermediate:
        return 'Intermediate';
      case ExperienceLevel.advanced:
        return 'Advanced';
      case ExperienceLevel.expert:
        return 'Expert';
    }
  }

  String get serverValue {
    switch (this) {
      case ExperienceLevel.beginner:
        return 'Beginner';
      case ExperienceLevel.intermediate:
        return 'Intermediate';
      case ExperienceLevel.advanced:
        return 'Advanced';
      case ExperienceLevel.expert:
        return 'Expert';
    }
  }

  static ExperienceLevel? fromString(String? value) {
    if (value == null) return null;
    switch (value) {
      case 'Beginner':
        return ExperienceLevel.beginner;
      case 'Intermediate':
        return ExperienceLevel.intermediate;
      case 'Advanced':
        return ExperienceLevel.advanced;
      case 'Expert':
        return ExperienceLevel.expert;
      default:
        return null;
    }
  }
}

enum FitnessGoal {
  weightLoss,
  muscleGain,
  strength,
  endurance,
  generalFitness;

  String get displayName {
    switch (this) {
      case FitnessGoal.weightLoss:
        return 'Weight Loss';
      case FitnessGoal.muscleGain:
        return 'Muscle Gain';
      case FitnessGoal.strength:
        return 'Strength';
      case FitnessGoal.endurance:
        return 'Endurance';
      case FitnessGoal.generalFitness:
        return 'General Fitness';
    }
  }

  String get serverValue {
    switch (this) {
      case FitnessGoal.weightLoss:
        return 'WeightLoss';
      case FitnessGoal.muscleGain:
        return 'MuscleGain';
      case FitnessGoal.strength:
        return 'Strength';
      case FitnessGoal.endurance:
        return 'Endurance';
      case FitnessGoal.generalFitness:
        return 'GeneralFitness';
    }
  }

  static FitnessGoal? fromString(String? value) {
    if (value == null) return null;
    switch (value) {
      case 'WeightLoss':
        return FitnessGoal.weightLoss;
      case 'MuscleGain':
        return FitnessGoal.muscleGain;
      case 'Strength':
        return FitnessGoal.strength;
      case 'Endurance':
        return FitnessGoal.endurance;
      case 'GeneralFitness':
        return FitnessGoal.generalFitness;
      default:
        return null;
    }
  }
}

enum UnitPreference {
  metric,
  imperial;

  String get displayName {
    switch (this) {
      case UnitPreference.metric:
        return 'Metric (kg, cm)';
      case UnitPreference.imperial:
        return 'Imperial (lbs, ft/in)';
    }
  }

  String get serverValue {
    switch (this) {
      case UnitPreference.metric:
        return 'Metric';
      case UnitPreference.imperial:
        return 'Imperial';
    }
  }

  static UnitPreference fromString(String? value) {
    if (value == 'Imperial') return UnitPreference.imperial;
    return UnitPreference.metric; // default
  }
}
