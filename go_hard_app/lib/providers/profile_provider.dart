import 'dart:io';
import 'dart:async';
import 'package:flutter/material.dart';
import '../data/models/user.dart';
import '../data/models/profile_update_request.dart';
import '../data/repositories/profile_repository.dart';
import '../data/services/auth_service.dart';
import '../core/services/connectivity_service.dart';

/// Provider for user profile management
/// Replaces ProfileViewModel from MAUI app
class ProfileProvider extends ChangeNotifier {
  final ProfileRepository _profileRepository;
  final AuthService _authService;
  final ConnectivityService? _connectivity;

  User? _currentUser;
  bool _isLoading = false;
  bool _isUpdating = false;
  bool _isUploadingPhoto = false;
  String? _errorMessage;
  String? _cachedThemePreference; // Theme loaded from local storage

  StreamSubscription<bool>? _connectivitySubscription;

  ProfileProvider(
    this._profileRepository,
    this._authService, [
    this._connectivity,
  ]) {
    // Load theme from local storage on init
    _loadCachedTheme();

    // Listen for connectivity changes and refresh when going online
    _connectivitySubscription = _connectivity?.connectivityStream.listen((
      isOnline,
    ) {
      if (isOnline && _currentUser != null) {
        debugPrint('📡 Connection restored - refreshing profile');
        loadUserProfile();
      }
    });
  }

  /// Load theme preference from local storage (fast, offline-first)
  Future<void> _loadCachedTheme() async {
    _cachedThemePreference = await _authService.getThemePreference();
    notifyListeners();
  }

  // Getters
  User? get currentUser => _currentUser;
  bool get isLoading => _isLoading;
  bool get isUpdating => _isUpdating;
  bool get isUploadingPhoto => _isUploadingPhoto;
  String? get errorMessage => _errorMessage;

  /// Get current theme mode based on user preference
  /// Uses cached theme from local storage first (offline-first)
  ThemeMode get themeMode {
    // First check cached theme (from local storage - fast!)
    final preference = _cachedThemePreference ?? _currentUser?.themePreference;
    switch (preference?.toLowerCase()) {
      case 'light':
        return ThemeMode.light;
      case 'dark':
        return ThemeMode.dark;
      default:
        return ThemeMode.system;
    }
  }

  /// Load current user profile with stats
  Future<void> loadUserProfile() async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      _currentUser = await _profileRepository.getProfile();

      // Save theme preference to local storage for offline access
      if (_currentUser?.themePreference != null) {
        _cachedThemePreference = _currentUser!.themePreference;
        await _authService.saveThemePreference(_currentUser!.themePreference!);
      }
    } catch (e) {
      _errorMessage =
          'Failed to load profile: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Load profile error: $e');
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  /// Update user profile
  Future<bool> updateProfile(ProfileUpdateRequest request) async {
    _isUpdating = true;
    _errorMessage = null;
    notifyListeners();

    try {
      _currentUser = await _profileRepository.updateProfile(request);

      // Save theme preference to local storage if updated
      if (_currentUser?.themePreference != null) {
        _cachedThemePreference = _currentUser!.themePreference;
        await _authService.saveThemePreference(_currentUser!.themePreference!);
      }

      _isUpdating = false;
      notifyListeners();
      return true;
    } catch (e) {
      _errorMessage =
          'Failed to update profile: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Update profile error: $e');
      _isUpdating = false;
      notifyListeners();
      return false;
    }
  }

  /// Upload profile photo
  Future<bool> uploadProfilePhoto(File imageFile) async {
    _isUploadingPhoto = true;
    _errorMessage = null;
    notifyListeners();

    try {
      await _profileRepository.uploadProfilePhoto(imageFile);

      // Reload profile to get updated photo URL
      await loadUserProfile();

      _isUploadingPhoto = false;
      notifyListeners();
      return true;
    } catch (e) {
      _errorMessage =
          'Failed to upload photo: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Upload photo error: $e');
      _isUploadingPhoto = false;
      notifyListeners();
      return false;
    }
  }

  /// Delete profile photo
  Future<bool> deleteProfilePhoto() async {
    _isUploadingPhoto = true;
    _errorMessage = null;
    notifyListeners();

    try {
      await _profileRepository.deleteProfilePhoto();

      // Reload profile to get updated data
      await loadUserProfile();

      _isUploadingPhoto = false;
      notifyListeners();
      return true;
    } catch (e) {
      _errorMessage =
          'Failed to delete photo: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Delete photo error: $e');
      _isUploadingPhoto = false;
      notifyListeners();
      return false;
    }
  }

  /// Toggle unit preference between Metric and Imperial
  Future<void> toggleUnitPreference() async {
    if (_currentUser == null) return;

    final currentPreference = _currentUser!.unitPreference ?? 'Metric';
    final newPreference = currentPreference == 'Metric' ? 'Imperial' : 'Metric';

    final request = ProfileUpdateRequest(unitPreference: newPreference);

    await updateProfile(request);
  }

  /// Get user name from auth service (cached)
  Future<String> getUserName() async {
    return await _authService.getUserName() ?? 'User';
  }

  /// Get user email from auth service (cached)
  Future<String> getUserEmail() async {
    return await _authService.getUserEmail() ?? '';
  }

  /// Clear error message
  void clearError() {
    _errorMessage = null;
    notifyListeners();
  }

  /// Clear all profile data (called on logout)
  void clear() {
    _currentUser = null;
    _errorMessage = null;
    _isLoading = false;
    _isUpdating = false;
    _isUploadingPhoto = false;
    // Keep theme preference for UX
    notifyListeners();
    debugPrint('🧹 ProfileProvider cleared');
  }

  @override
  void dispose() {
    _connectivitySubscription?.cancel();
    super.dispose();
  }
}
