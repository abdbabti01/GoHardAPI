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

      // Color Scheme - GoHard Brand
      colorScheme: ColorScheme.light(
        primary: AppColors.goHardBlack,
        secondary: AppColors.iosSystemGreen,
        tertiary: AppColors.goHardDarkGray,
        error: AppColors.errorRed,
        surface: AppColors.iosSystemBackground,
        surfaceContainerHighest: AppColors.iosGray1,
        onPrimary: AppColors.goHardWhite,
        onSecondary: AppColors.goHardWhite,
        onTertiary: AppColors.goHardWhite,
        onError: AppColors.goHardWhite,
        onSurface: AppColors.goHardBlack,
        onSurfaceVariant: AppColors.goHardDarkGray,
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
        selectedItemColor: AppColors.goHardBlack,
        unselectedItemColor: AppColors.iosGray6,
        showUnselectedLabels: true,
        type: BottomNavigationBarType.fixed,
        elevation: 0,
      ),

      // Floating Action Button
      floatingActionButtonTheme: const FloatingActionButtonThemeData(
        backgroundColor: AppColors.goHardBlack,
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
          borderSide: const BorderSide(color: AppColors.goHardBlack, width: 2),
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
          backgroundColor: AppColors.goHardBlack,
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
          foregroundColor: AppColors.goHardBlack,
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
        selectedColor: AppColors.goHardBlack,
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

      // Color Scheme - GoHard Brand (Dark) - White on Black like logo
      colorScheme: ColorScheme.dark(
        primary: AppColors.goHardWhite,
        secondary: AppColors.iosSystemGreen,
        tertiary: AppColors.goHardSilver,
        error: AppColors.errorRed,
        surface: AppColors.goHardBlack,
        surfaceContainerHighest: AppColors.goHardDarkGray,
        onPrimary: AppColors.goHardBlack,
        onSecondary: AppColors.goHardBlack,
        onTertiary: AppColors.goHardBlack,
        onError: AppColors.goHardBlack,
        onSurface: AppColors.goHardWhite,
        onSurfaceVariant: AppColors.goHardSilver,
      ),

      // Scaffold
      scaffoldBackgroundColor: AppColors.goHardBlack,

      // AppBar
      appBarTheme: const AppBarTheme(
        backgroundColor: AppColors.goHardBlack,
        foregroundColor: AppColors.goHardWhite,
        elevation: 0,
        centerTitle: false,
        titleTextStyle: TextStyle(
          color: AppColors.goHardWhite,
          fontSize: 34,
          fontWeight: FontWeight.bold,
        ),
      ),

      // Card
      cardTheme: CardTheme(
        color: AppColors.goHardDarkGray,
        elevation: 2,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        margin: const EdgeInsets.symmetric(horizontal: 20, vertical: 8),
      ),

      // Bottom Navigation Bar
      bottomNavigationBarTheme: const BottomNavigationBarThemeData(
        backgroundColor: AppColors.goHardBlack,
        selectedItemColor: AppColors.goHardWhite,
        unselectedItemColor: AppColors.goHardSilver,
        showUnselectedLabels: true,
        type: BottomNavigationBarType.fixed,
        elevation: 0,
      ),

      // Floating Action Button
      floatingActionButtonTheme: const FloatingActionButtonThemeData(
        backgroundColor: AppColors.goHardWhite,
        foregroundColor: AppColors.goHardBlack,
        elevation: 4,
        shape: CircleBorder(),
      ),

      // Input Decoration
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: AppColors.goHardDarkGray,
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: BorderSide.none,
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(12),
          borderSide: const BorderSide(color: AppColors.goHardWhite, width: 2),
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
          backgroundColor: AppColors.goHardWhite,
          foregroundColor: AppColors.goHardBlack,
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
          foregroundColor: AppColors.goHardWhite,
          textStyle: const TextStyle(fontSize: 17, fontWeight: FontWeight.w600),
        ),
      ),

      // Text Theme
      textTheme: const TextTheme(
        displayLarge: TextStyle(
          fontSize: 34,
          fontWeight: FontWeight.bold,
          color: AppColors.goHardWhite,
        ),
        displayMedium: TextStyle(
          fontSize: 28,
          fontWeight: FontWeight.bold,
          color: AppColors.goHardWhite,
        ),
        displaySmall: TextStyle(
          fontSize: 22,
          fontWeight: FontWeight.bold,
          color: AppColors.goHardWhite,
        ),
        headlineMedium: TextStyle(
          fontSize: 20,
          fontWeight: FontWeight.w600,
          color: AppColors.goHardWhite,
        ),
        titleLarge: TextStyle(
          fontSize: 18,
          fontWeight: FontWeight.bold,
          color: AppColors.goHardWhite,
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
        backgroundColor: AppColors.goHardDarkGray,
        selectedColor: AppColors.goHardWhite,
        labelStyle: const TextStyle(color: AppColors.goHardWhite),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
      ),
    );
  }
}
