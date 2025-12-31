import '../../core/constants/api_config.dart';
import '../models/auth_response.dart';
import '../models/login_request.dart';
import '../models/signup_request.dart';
import '../services/api_service.dart';

/// Repository for authentication operations
class AuthRepository {
  final ApiService _apiService;

  AuthRepository(this._apiService);

  /// Login user and return authentication response
  Future<AuthResponse> login(LoginRequest request) async {
    final data = await _apiService.post<Map<String, dynamic>>(
      ApiConfig.authLogin,
      data: request.toJson(),
    );
    return AuthResponse.fromJson(data);
  }

  /// Register new user and return authentication response
  Future<AuthResponse> signup(SignupRequest request) async {
    final data = await _apiService.post<Map<String, dynamic>>(
      ApiConfig.authSignup,
      data: request.toJson(),
    );
    return AuthResponse.fromJson(data);
  }
}
