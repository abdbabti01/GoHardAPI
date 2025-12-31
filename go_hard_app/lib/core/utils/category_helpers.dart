import 'package:flutter/material.dart';
import '../constants/colors.dart';

/// Helper functions for exercise categories
/// Matches the CategoryToColorConverter and CategoryToIconConverter from MAUI app
class CategoryHelpers {
  CategoryHelpers._(); // Private constructor to prevent instantiation

  /// Get the color for a given exercise category
  /// Matches CategoryToColorConverter.cs logic
  static Color getCategoryColor(String? category) {
    if (category == null || category.isEmpty) {
      return AppColors.defaultCategory;
    }

    switch (category.toLowerCase()) {
      case 'strength':
        return AppColors.strengthRed;
      case 'cardio':
        return AppColors.cardioBlue;
      case 'flexibility':
        return AppColors.flexibilityGreen;
      case 'core':
        return AppColors.coreOrange;
      default:
        return AppColors.defaultCategory;
    }
  }

  /// Get the icon emoji for a given exercise category
  /// Matches CategoryToIconConverter.cs logic
  static String getCategoryIcon(String? category) {
    if (category == null || category.isEmpty) {
      return '🏋️';
    }

    switch (category.toLowerCase()) {
      case 'strength':
        return '💪';
      case 'cardio':
        return '❤️';
      case 'flexibility':
        return '🧘';
      case 'core':
        return '⚡';
      default:
        return '🏋️';
    }
  }

  /// Get a lighter version of the category color for backgrounds
  static Color getCategoryColorLight(String? category) {
    return getCategoryColor(category).withValues(alpha: 0.1);
  }

  /// Get category display name (capitalized)
  static String getCategoryDisplayName(String? category) {
    if (category == null || category.isEmpty) {
      return 'Unknown';
    }
    return category[0].toUpperCase() + category.substring(1).toLowerCase();
  }

  /// Get all available categories
  static List<String> get allCategories => [
    'All',
    'Strength',
    'Cardio',
    'Core',
    'Flexibility',
  ];
}
