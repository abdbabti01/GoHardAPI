import 'dart:io';
import 'package:flutter/foundation.dart';
import '../data/models/user.dart';
import '../data/models/profile_update_request.dart';
import '../data/repositories/user_repository.dart';
import '../data/repositories/profile_repository.dart';
import '../data/services/auth_service.dart';

/// Provider for user profile management
/// Replaces ProfileViewModel from MAUI app
class ProfileProvider extends ChangeNotifier {
  final UserRepository _userRepository;
  final ProfileRepository _profileRepository;
  final AuthService _authService;

  User? _currentUser;
  bool _isLoading = false;
  bool _isUpdating = false;
  bool _isUploadingPhoto = false;
  String? _errorMessage;

  ProfileProvider(
    this._userRepository,
    this._profileRepository,
    this._authService,
  );

  // Getters
  User? get currentUser => _currentUser;
  bool get isLoading => _isLoading;
  bool get isUpdating => _isUpdating;
  bool get isUploadingPhoto => _isUploadingPhoto;
  String? get errorMessage => _errorMessage;

  /// Load current user profile with stats
  Future<void> loadUserProfile() async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      _currentUser = await _profileRepository.getProfile();
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
      final photoUrl = await _profileRepository.uploadProfilePhoto(imageFile);

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

    final request = ProfileUpdateRequest(
      unitPreference: newPreference,
    );

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
}
