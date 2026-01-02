class UnitConverter {
  // Weight conversion constants
  static const double kgToLbsMultiplier = 2.20462;

  // Height conversion constants
  static const double cmToInchesMultiplier = 0.393701;
  static const double inchesPerFoot = 12.0;

  // ====================
  // Weight Conversions
  // ====================

  /// Convert kilograms to pounds
  static double kgToLbs(double kg) {
    return kg * kgToLbsMultiplier;
  }

  /// Convert pounds to kilograms
  static double lbsToKg(double lbs) {
    return lbs / kgToLbsMultiplier;
  }

  // ====================
  // Height Conversions
  // ====================

  /// Convert centimeters to inches
  static double cmToInches(double cm) {
    return cm * cmToInchesMultiplier;
  }

  /// Convert inches to centimeters
  static double inchesToCm(double inches) {
    return inches / cmToInchesMultiplier;
  }

  /// Convert centimeters to feet and inches
  /// Returns a Map with 'feet' and 'inches' keys
  static Map<String, int> cmToFeetInches(double cm) {
    final totalInches = cmToInches(cm);
    final feet = (totalInches / inchesPerFoot).floor();
    final inches = (totalInches % inchesPerFoot).round();

    return {'feet': feet, 'inches': inches};
  }

  /// Convert feet and inches to centimeters
  static double feetInchesToCm(int feet, int inches) {
    final totalInches = (feet * inchesPerFoot) + inches;
    return inchesToCm(totalInches);
  }

  // ====================
  // Formatting Methods
  // ====================

  /// Format weight for display based on unit preference
  /// Returns formatted string like "70.5 kg" or "155.4 lbs"
  static String formatWeight(double? weight, String unitPreference) {
    if (weight == null) return '--';

    if (unitPreference == 'Imperial') {
      final lbs = kgToLbs(weight);
      return '${lbs.toStringAsFixed(1)} lbs';
    } else {
      return '${weight.toStringAsFixed(1)} kg';
    }
  }

  /// Format height for display based on unit preference
  /// Returns formatted string like "175 cm" or "5'9\""
  static String formatHeight(double? height, String unitPreference) {
    if (height == null) return '--';

    if (unitPreference == 'Imperial') {
      final feetInches = cmToFeetInches(height);
      return '${feetInches['feet']}\'${feetInches['inches']}"';
    } else {
      return '${height.toStringAsFixed(0)} cm';
    }
  }

  /// Format weight with unit label (for input fields)
  /// Returns just the unit like "kg" or "lbs"
  static String getWeightUnit(String unitPreference) {
    return unitPreference == 'Imperial' ? 'lbs' : 'kg';
  }

  /// Format height with unit label (for input fields)
  /// Returns just the unit like "cm" or "ft/in"
  static String getHeightUnit(String unitPreference) {
    return unitPreference == 'Imperial' ? 'ft/in' : 'cm';
  }

  // ====================
  // Input Conversion Helpers
  // ====================

  /// Convert user input weight to metric (kg) for backend storage
  /// If user is in Imperial mode, input is in lbs and needs conversion
  static double? convertInputWeightToMetric(
    double? inputValue,
    String unitPreference,
  ) {
    if (inputValue == null) return null;

    if (unitPreference == 'Imperial') {
      return lbsToKg(inputValue);
    } else {
      return inputValue; // already in kg
    }
  }

  /// Convert user input height to metric (cm) for backend storage
  /// If user is in Imperial mode, input needs conversion from ft/in
  static double? convertInputHeightToMetric(
    double? inputValue,
    String unitPreference,
  ) {
    if (inputValue == null) return null;

    if (unitPreference == 'Imperial') {
      return inchesToCm(inputValue); // assuming input is total inches
    } else {
      return inputValue; // already in cm
    }
  }

  /// Convert metric weight (kg) from backend to display value
  /// If user is in Imperial mode, convert to lbs
  static double? convertMetricWeightToDisplay(
    double? metricValue,
    String unitPreference,
  ) {
    if (metricValue == null) return null;

    if (unitPreference == 'Imperial') {
      return kgToLbs(metricValue);
    } else {
      return metricValue;
    }
  }

  /// Convert metric height (cm) from backend to display value
  /// If user is in Imperial mode, convert to total inches
  static double? convertMetricHeightToDisplay(
    double? metricValue,
    String unitPreference,
  ) {
    if (metricValue == null) return null;

    if (unitPreference == 'Imperial') {
      return cmToInches(metricValue);
    } else {
      return metricValue;
    }
  }
}
