import 'package:flutter/material.dart';
import '../constants/colors.dart';

/// App theme configuration matching MAUI app design
class AppTheme {
  AppTheme._(); // Private constructor to prevent instantiation

  /// Light theme configuration
  static ThemeData get lightTheme {
    return ThemeData(
      useMaterial3: true,
      brightness: Brightness.light,

      // Color Scheme
      colorScheme: ColorScheme.light(
        primary: AppColors.iosSystemBlue,
        secondary: AppColors.iosSystemGreen,
        error: AppColors.errorRed,
        surface: AppColors.iosSystemBackground,
        surfaceContainerHighest: AppColors.iosGray1,
        onPrimary: Colors.white,
        onSecondary: Colors.white,
        onError: Colors.white,
        onSurface: AppColors.iosLabel,
      ),

      // Scaffold
      scaffoldBackgroundColor: AppColors.iosSystemBackground,

      // AppBar
      appBarTheme: const AppBarTheme(
        backgroundColor: AppColors.iosSystemBackground,
        foregroundColor: AppColors.iosLabel,
        elevation: 0,
        centerTitle: false,
        titleTextStyle: TextStyle(
          color: AppColors.iosLabel,
          fontSize: 34,
          fontWeight: FontWeight.bold,
        ),
      ),

      // Card
      cardTheme: CardTheme(
        color: AppColors.cardBackgroundLight,
        elevation: 2,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        margin: const EdgeInsets.symmetric(horizontal: 20, vertical: 8),
      ),

      // Bottom Navigation Bar
      bottomNavigationBarTheme: const BottomNavigationBarThemeData(
        backgroundColor: AppColors.iosSystemBackground,
        selectedItemColor: AppColors.iosSystemBlue,
        unselectedItemColor: AppColors.iosGray6,
        showUnselectedLabels: true,
        type: BottomNavigationBarType.fixed,
        elevation: 0,
      ),

      // Floating Action Button
      floatingActionButtonTheme: const FloatingActionButtonThemeData(
        backgroundColor: AppColors.fitnessGradientStart,
        foregroundColor: Colors.white,
        elevation: 4,
        shape: CircleBorder(),
      ),

      // Input Decoration
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: AppColors.iosGray1,
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: BorderSide.none,
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(
            color: AppColors.iosSystemBlue,
            width: 2,
          ),
        ),
        errorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: AppColors.errorRed, width: 2),
        ),
        contentPadding: const EdgeInsets.symmetric(
          horizontal: 16,
          vertical: 16,
        ),
      ),

      // Elevated Button
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: AppColors.iosSystemBlue,
          foregroundColor: Colors.white,
          elevation: 0,
          padding: const EdgeInsets.symmetric(vertical: 16, horizontal: 24),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
          textStyle: const TextStyle(fontSize: 17, fontWeight: FontWeight.w600),
        ),
      ),

      // Text Button
      textButtonTheme: TextButtonThemeData(
        style: TextButton.styleFrom(
          foregroundColor: AppColors.iosSystemBlue,
          textStyle: const TextStyle(fontSize: 17, fontWeight: FontWeight.w600),
        ),
      ),

      // Text Theme
      textTheme: const TextTheme(
        displayLarge: TextStyle(
          fontSize: 34,
          fontWeight: FontWeight.bold,
          color: AppColors.iosLabel,
        ),
        displayMedium: TextStyle(
          fontSize: 28,
          fontWeight: FontWeight.bold,
          color: AppColors.iosLabel,
        ),
        displaySmall: TextStyle(
          fontSize: 22,
          fontWeight: FontWeight.bold,
          color: AppColors.iosLabel,
        ),
        headlineMedium: TextStyle(
          fontSize: 20,
          fontWeight: FontWeight.w600,
          color: AppColors.iosLabel,
        ),
        titleLarge: TextStyle(
          fontSize: 18,
          fontWeight: FontWeight.bold,
          color: AppColors.iosLabel,
        ),
        bodyLarge: TextStyle(fontSize: 17, color: AppColors.iosLabel),
        bodyMedium: TextStyle(fontSize: 15, color: AppColors.iosSecondaryLabel),
        bodySmall: TextStyle(fontSize: 13, color: AppColors.iosTertiaryLabel),
      ),

      // Divider
      dividerTheme: const DividerThemeData(
        color: AppColors.iosGray3,
        thickness: 0.5,
        space: 1,
      ),

      // Chip
      chipTheme: ChipThemeData(
        backgroundColor: AppColors.iosGray1,
        selectedColor: AppColors.iosSystemBlue,
        labelStyle: const TextStyle(color: AppColors.iosLabel),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
      ),
    );
  }

  /// Dark theme configuration
  static ThemeData get darkTheme {
    return ThemeData(
      useMaterial3: true,
      brightness: Brightness.dark,

      // Color Scheme
      colorScheme: ColorScheme.dark(
        primary: AppColors.iosSystemBlue,
        secondary: AppColors.iosSystemGreen,
        error: AppColors.errorRed,
        surface: AppColors.iosDarkSystemBackground,
        surfaceContainerHighest: AppColors.iosDarkGray1,
        onPrimary: Colors.white,
        onSecondary: Colors.white,
        onError: Colors.white,
        onSurface: AppColors.iosDarkLabel,
      ),

      // Scaffold
      scaffoldBackgroundColor: AppColors.iosDarkSystemBackground,

      // AppBar
      appBarTheme: const AppBarTheme(
        backgroundColor: AppColors.iosDarkSystemBackground,
        foregroundColor: AppColors.iosDarkLabel,
        elevation: 0,
        centerTitle: false,
        titleTextStyle: TextStyle(
          color: AppColors.iosDarkLabel,
          fontSize: 34,
          fontWeight: FontWeight.bold,
        ),
      ),

      // Card
      cardTheme: CardTheme(
        color: AppColors.cardBackgroundDark,
        elevation: 2,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        margin: const EdgeInsets.symmetric(horizontal: 20, vertical: 8),
      ),

      // Bottom Navigation Bar
      bottomNavigationBarTheme: const BottomNavigationBarThemeData(
        backgroundColor: AppColors.iosDarkSystemBackground,
        selectedItemColor: AppColors.iosSystemBlue,
        unselectedItemColor: AppColors.iosGray5,
        showUnselectedLabels: true,
        type: BottomNavigationBarType.fixed,
        elevation: 0,
      ),

      // Floating Action Button
      floatingActionButtonTheme: const FloatingActionButtonThemeData(
        backgroundColor: AppColors.fitnessGradientStart,
        foregroundColor: Colors.white,
        elevation: 4,
        shape: CircleBorder(),
      ),

      // Input Decoration
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: AppColors.iosDarkGray2,
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: BorderSide.none,
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(
            color: AppColors.iosSystemBlue,
            width: 2,
          ),
        ),
        errorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: AppColors.errorRed, width: 2),
        ),
        contentPadding: const EdgeInsets.symmetric(
          horizontal: 16,
          vertical: 16,
        ),
      ),

      // Elevated Button
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: AppColors.iosSystemBlue,
          foregroundColor: Colors.white,
          elevation: 0,
          padding: const EdgeInsets.symmetric(vertical: 16, horizontal: 24),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
          textStyle: const TextStyle(fontSize: 17, fontWeight: FontWeight.w600),
        ),
      ),

      // Text Button
      textButtonTheme: TextButtonThemeData(
        style: TextButton.styleFrom(
          foregroundColor: AppColors.iosSystemBlue,
          textStyle: const TextStyle(fontSize: 17, fontWeight: FontWeight.w600),
        ),
      ),

      // Text Theme
      textTheme: const TextTheme(
        displayLarge: TextStyle(
          fontSize: 34,
          fontWeight: FontWeight.bold,
          color: AppColors.iosDarkLabel,
        ),
        displayMedium: TextStyle(
          fontSize: 28,
          fontWeight: FontWeight.bold,
          color: AppColors.iosDarkLabel,
        ),
        displaySmall: TextStyle(
          fontSize: 22,
          fontWeight: FontWeight.bold,
          color: AppColors.iosDarkLabel,
        ),
        headlineMedium: TextStyle(
          fontSize: 20,
          fontWeight: FontWeight.w600,
          color: AppColors.iosDarkLabel,
        ),
        titleLarge: TextStyle(
          fontSize: 18,
          fontWeight: FontWeight.bold,
          color: AppColors.iosDarkLabel,
        ),
        bodyLarge: TextStyle(fontSize: 17, color: AppColors.iosDarkLabel),
        bodyMedium: TextStyle(
          fontSize: 15,
          color: AppColors.iosDarkSecondaryLabel,
        ),
        bodySmall: TextStyle(
          fontSize: 13,
          color: AppColors.iosDarkTertiaryLabel,
        ),
      ),

      // Divider
      dividerTheme: const DividerThemeData(
        color: AppColors.iosDarkGray3,
        thickness: 0.5,
        space: 1,
      ),

      // Chip
      chipTheme: ChipThemeData(
        backgroundColor: AppColors.iosDarkGray2,
        selectedColor: AppColors.iosSystemBlue,
        labelStyle: const TextStyle(color: AppColors.iosDarkLabel),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
      ),
    );
  }
}
