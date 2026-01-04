import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// Service for managing authentication tokens and user data in secure storage
/// Matches the AuthService.cs from MAUI app
class AuthService {
  static const _storage = FlutterSecureStorage();

  static const String _tokenKey = 'jwt_token';
  static const String _userIdKey = 'user_id';
  static const String _userNameKey = 'user_name';
  static const String _userEmailKey = 'user_email';
  static const String _themePreferenceKey = 'theme_preference';
  static const String _cachedProfileKey = 'cached_user_profile';

  /// Save authentication data to secure storage
  Future<void> saveToken({
    required String token,
    required int userId,
    required String name,
    required String email,
  }) async {
    await Future.wait([
      _storage.write(key: _tokenKey, value: token),
      _storage.write(key: _userIdKey, value: userId.toString()),
      _storage.write(key: _userNameKey, value: name),
      _storage.write(key: _userEmailKey, value: email),
    ]);
  }

  /// Get JWT token from secure storage
  Future<String?> getToken() async {
    try {
      return await _storage.read(key: _tokenKey);
    } catch (e) {
      return null;
    }
  }

  /// Get user ID from secure storage
  Future<int?> getUserId() async {
    try {
      final userIdString = await _storage.read(key: _userIdKey);
      if (userIdString != null) {
        return int.tryParse(userIdString);
      }
      return null;
    } catch (e) {
      return null;
    }
  }

  /// Get user name from secure storage
  Future<String?> getUserName() async {
    try {
      return await _storage.read(key: _userNameKey);
    } catch (e) {
      return null;
    }
  }

  /// Get user email from secure storage
  Future<String?> getUserEmail() async {
    try {
      return await _storage.read(key: _userEmailKey);
    } catch (e) {
      return null;
    }
  }

  /// Check if user is authenticated (has valid token)
  Future<bool> isAuthenticated() async {
    final token = await getToken();
    return token != null && token.isNotEmpty;
  }

  /// Clear all authentication data from secure storage
  Future<void> clearToken() async {
    await Future.wait([
      _storage.delete(key: _tokenKey),
      _storage.delete(key: _userIdKey),
      _storage.delete(key: _userNameKey),
      _storage.delete(key: _userEmailKey),
      // Don't delete theme preference - user's theme choice persists across logins
    ]);
  }

  /// Save theme preference to secure storage
  Future<void> saveThemePreference(String theme) async {
    try {
      await _storage.write(key: _themePreferenceKey, value: theme);
    } catch (e) {
      // Fail silently - theme preference is not critical
    }
  }

  /// Get theme preference from secure storage
  Future<String?> getThemePreference() async {
    try {
      return await _storage.read(key: _themePreferenceKey);
    } catch (e) {
      return null; // Default to system theme
    }
  }

  /// Save user profile JSON to secure storage for offline access
  Future<void> saveCachedProfile(String profileJson) async {
    try {
      await _storage.write(key: _cachedProfileKey, value: profileJson);
    } catch (e) {
      // Fail silently - cache is not critical
    }
  }

  /// Get cached user profile JSON from secure storage
  Future<String?> getCachedProfile() async {
    try {
      return await _storage.read(key: _cachedProfileKey);
    } catch (e) {
      return null;
    }
  }
}
