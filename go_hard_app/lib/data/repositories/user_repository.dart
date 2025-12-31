import '../../core/constants/api_config.dart';
import '../models/user.dart';
import '../services/api_service.dart';

/// Repository for user operations
class UserRepository {
  final ApiService _apiService;

  UserRepository(this._apiService);

  /// Get all users
  Future<List<User>> getUsers() async {
    final data = await _apiService.get<List<dynamic>>(ApiConfig.users);
    return data
        .map((json) => User.fromJson(json as Map<String, dynamic>))
        .toList();
  }

  /// Get user by ID
  Future<User> getUser(int id) async {
    final data = await _apiService.get<Map<String, dynamic>>(
      ApiConfig.userById(id),
    );
    return User.fromJson(data);
  }

  /// Create new user
  Future<User> createUser(User user) async {
    final data = await _apiService.post<Map<String, dynamic>>(
      ApiConfig.users,
      data: user.toJson(),
    );
    return User.fromJson(data);
  }

  /// Update user
  Future<User> updateUser(int id, User user) async {
    final data = await _apiService.put<Map<String, dynamic>>(
      ApiConfig.userById(id),
      data: user.toJson(),
    );
    return User.fromJson(data);
  }

  /// Delete user
  Future<bool> deleteUser(int id) async {
    return await _apiService.delete(ApiConfig.userById(id));
  }
}
