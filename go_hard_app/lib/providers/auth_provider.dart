import 'package:flutter/foundation.dart';
import '../data/models/login_request.dart';
import '../data/models/signup_request.dart';
import '../data/repositories/auth_repository.dart';
import '../data/services/auth_service.dart';
import '../data/local/services/local_database_service.dart';

/// Provider for authentication state management
/// Combines LoginViewModel and SignupViewModel from MAUI app
class AuthProvider extends ChangeNotifier {
  final AuthRepository _authRepository;
  final AuthService _authService;
  final LocalDatabaseService _localDb;

  // Login fields
  String _email = '';
  String _password = '';

  // Signup fields
  String _signupName = '';
  String _signupEmail = '';
  String _signupPassword = '';
  String _signupConfirmPassword = '';

  // UI state
  bool _isLoading = false;
  String _errorMessage = '';
  bool _isInitializing = true; // Track if initial auth check is in progress

  // Authentication state
  bool _isAuthenticated = false;
  int? _currentUserId;
  String? _currentUserName;
  String? _currentUserEmail;

  AuthProvider(this._authRepository, this._authService, this._localDb) {
    _checkAuthStatus();
  }

  // Getters
  String get email => _email;
  String get password => _password;
  String get signupName => _signupName;
  String get signupEmail => _signupEmail;
  String get signupPassword => _signupPassword;
  String get signupConfirmPassword => _signupConfirmPassword;
  bool get isLoading => _isLoading;
  String get errorMessage => _errorMessage;
  bool get isInitializing => _isInitializing;
  bool get isAuthenticated => _isAuthenticated;
  int? get currentUserId => _currentUserId;
  String? get currentUserName => _currentUserName;
  String? get currentUserEmail => _currentUserEmail;

  // Setters
  void setEmail(String value) {
    _email = value;
    notifyListeners();
  }

  void setPassword(String value) {
    _password = value;
    notifyListeners();
  }

  void setSignupName(String value) {
    _signupName = value;
    notifyListeners();
  }

  void setSignupEmail(String value) {
    _signupEmail = value;
    notifyListeners();
  }

  void setSignupPassword(String value) {
    _signupPassword = value;
    notifyListeners();
  }

  void setSignupConfirmPassword(String value) {
    _signupConfirmPassword = value;
    notifyListeners();
  }

  /// Check if user is already authenticated on app start
  Future<void> _checkAuthStatus() async {
    try {
      _isAuthenticated = await _authService.isAuthenticated();
      if (_isAuthenticated) {
        _currentUserId = await _authService.getUserId();
        _currentUserName = await _authService.getUserName();
        _currentUserEmail = await _authService.getUserEmail();
      }
    } finally {
      _isInitializing = false;
      notifyListeners();
    }
  }

  /// Login user
  Future<bool> login() async {
    if (_email.trim().isEmpty || _password.isEmpty) {
      _errorMessage = 'Please enter both email and password';
      notifyListeners();
      return false;
    }

    _isLoading = true;
    _errorMessage = '';
    notifyListeners();

    try {
      final request = LoginRequest(email: _email.trim(), password: _password);

      final response = await _authRepository.login(request);

      // Save token and user info to secure storage
      await _authService.saveToken(
        token: response.token,
        userId: response.userId,
        name: response.name,
        email: response.email,
      );

      // Update local state
      _isAuthenticated = true;
      _currentUserId = response.userId;
      _currentUserName = response.name;
      _currentUserEmail = response.email;

      // Clear password for security
      _password = '';

      return true;
    } catch (e) {
      _errorMessage =
          'Login failed: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Login error: $e');
      return false;
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  /// Signup new user
  Future<bool> signup() async {
    // Validation
    if (_signupName.trim().isEmpty) {
      _errorMessage = 'Please enter your name';
      notifyListeners();
      return false;
    }

    if (_signupEmail.trim().isEmpty) {
      _errorMessage = 'Please enter your email';
      notifyListeners();
      return false;
    }

    if (_signupPassword.length < 6) {
      _errorMessage = 'Password must be at least 6 characters';
      notifyListeners();
      return false;
    }

    if (_signupPassword != _signupConfirmPassword) {
      _errorMessage = 'Passwords do not match';
      notifyListeners();
      return false;
    }

    _isLoading = true;
    _errorMessage = '';
    notifyListeners();

    try {
      final request = SignupRequest(
        name: _signupName.trim(),
        email: _signupEmail.trim(),
        password: _signupPassword,
      );

      final response = await _authRepository.signup(request);

      // Save token and user info to secure storage
      await _authService.saveToken(
        token: response.token,
        userId: response.userId,
        name: response.name,
        email: response.email,
      );

      // Update local state
      _isAuthenticated = true;
      _currentUserId = response.userId;
      _currentUserName = response.name;
      _currentUserEmail = response.email;

      // Clear passwords for security
      _signupPassword = '';
      _signupConfirmPassword = '';

      return true;
    } catch (e) {
      _errorMessage =
          'Signup failed: ${e.toString().replaceAll('Exception: ', '')}';
      debugPrint('Signup error: $e');
      return false;
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  /// Logout user
  Future<void> logout() async {
    // Clear authentication token
    await _authService.clearToken();

    // Clear all local database data for privacy/security
    try {
      await _localDb.clearAll();
      debugPrint('✅ Local database cleared on logout');
    } catch (e) {
      debugPrint('⚠️ Failed to clear local database: $e');
    }

    // Clear local state
    _isAuthenticated = false;
    _currentUserId = null;
    _currentUserName = null;
    _currentUserEmail = null;
    _email = '';
    _password = '';
    _signupName = '';
    _signupEmail = '';
    _signupPassword = '';
    _signupConfirmPassword = '';
    _errorMessage = '';
    notifyListeners();
  }

  /// Clear error message
  void clearError() {
    _errorMessage = '';
    notifyListeners();
  }

  /// Set error message manually
  void setError(String message) {
    _errorMessage = message;
    notifyListeners();
  }

  /// Convenience methods for updating fields
  void updateEmail(String value) {
    _email = value;
    _signupEmail = value;
  }

  void updatePassword(String value) {
    _password = value;
    _signupPassword = value;
  }

  void updateName(String value) {
    _signupName = value;
  }
}
