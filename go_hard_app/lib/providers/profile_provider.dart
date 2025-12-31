import 'package:flutter/foundation.dart';
import '../data/models/user.dart';
import '../data/repositories/user_repository.dart';
import '../data/services/auth_service.dart';

/// Provider for user profile management
/// Replaces ProfileViewModel from MAUI app
class ProfileProvider extends ChangeNotifier {
  final UserRepository _userRepository;
  final AuthService _authService;

  User? _currentUser;
  bool _isLoading = false;
  String? _errorMessage;

  ProfileProvider(this._userRepository, this._authService);

  // Getters
  User? get currentUser => _currentUser;
  bool get isLoading => _isLoading;
  String? get errorMessage => _errorMessage;

  /// Load current user profile
  Future<void> loadUserProfile() async {
    final userId = await _authService.getUserId();
    if (userId == null) {
      _errorMessage = 'User not authenticated';
      notifyListeners();
      return;
    }

    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      _currentUser = await _userRepository.getUser(userId);
    } catch (e) {
      _errorMessage =
          'Failed to load profile: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Load profile error: $e');
    } finally {
      _isLoading = false;
      notifyListeners();
    }
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
