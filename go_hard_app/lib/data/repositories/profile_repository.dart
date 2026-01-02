import 'dart:io';
import 'package:dio/dio.dart';
import '../../core/constants/api_config.dart';
import '../models/user.dart';
import '../models/profile_update_request.dart';
import '../services/api_service.dart';
import '../services/auth_service.dart';

/// Repository for profile operations
class ProfileRepository {
  final ApiService _apiService;
  final AuthService _authService;

  ProfileRepository(this._apiService, this._authService);

  /// Get current user's profile with stats
  Future<User> getProfile() async {
    final data = await _apiService.get<Map<String, dynamic>>(
      ApiConfig.profile,
    );
    return User.fromJson(data);
  }

  /// Update current user's profile
  Future<User> updateProfile(ProfileUpdateRequest request) async {
    final data = await _apiService.put<Map<String, dynamic>>(
      ApiConfig.profile,
      data: request.toJson(),
    );
    return User.fromJson(data);
  }

  /// Upload profile photo
  /// Returns the photo URL on success
  Future<String> uploadProfilePhoto(File imageFile) async {
    try {
      // Get token for manual request (Dio interceptor will add it)
      final token = await _authService.getToken();

      // Create multipart form data
      final formData = FormData.fromMap({
        'photo': await MultipartFile.fromFile(
          imageFile.path,
          filename: imageFile.path.split('/').last,
        ),
      });

      // Make manual Dio request with multipart form data
      final dio = Dio(
        BaseOptions(
          baseUrl: ApiConfig.baseUrl,
          connectTimeout: ApiConfig.connectTimeout,
          receiveTimeout: ApiConfig.receiveTimeout,
          headers: {
            'Authorization': 'Bearer $token',
          },
        ),
      );

      final response = await dio.post<Map<String, dynamic>>(
        ApiConfig.profilePhoto,
        data: formData,
      );

      // Parse response
      if (response.data != null && response.data!['photoUrl'] != null) {
        return response.data!['photoUrl'] as String;
      } else {
        throw Exception('Failed to upload photo - no URL returned');
      }
    } on DioException catch (e) {
      if (e.response != null) {
        final message =
            e.response?.data?['message'] ??
            e.response?.data?['title'] ??
            e.response?.statusMessage ??
            'Failed to upload photo';
        throw Exception('Upload failed: $message');
      } else {
        throw Exception('Upload failed: ${e.message}');
      }
    }
  }

  /// Delete profile photo
  Future<bool> deleteProfilePhoto() async {
    return await _apiService.delete(ApiConfig.profilePhoto);
  }
}
