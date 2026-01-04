import 'dart:io';
import 'dart:convert';
import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';
import '../../core/constants/api_config.dart';
import '../../core/services/connectivity_service.dart';
import '../models/user.dart';
import '../models/profile_update_request.dart';
import '../services/api_service.dart';
import '../services/auth_service.dart';

/// Repository for profile operations with offline caching
class ProfileRepository {
  final ApiService _apiService;
  final AuthService _authService;
  final ConnectivityService? _connectivity;

  ProfileRepository(this._apiService, this._authService, [this._connectivity]);

  /// Get current user's profile with stats
  /// Offline-first: returns cached profile when offline
  Future<User> getProfile() async {
    final isOnline = _connectivity?.isOnline ?? true;

    if (isOnline) {
      try {
        final data = await _apiService.get<Map<String, dynamic>>(
          ApiConfig.profile,
        );
        final user = User.fromJson(data);

        // Cache the profile for offline use
        await _cacheProfile(user);

        return user;
      } catch (e) {
        debugPrint('⚠️ API failed, falling back to cached profile: $e');
        return await _getCachedProfile();
      }
    } else {
      debugPrint('📴 Offline - returning cached profile');
      return await _getCachedProfile();
    }
  }

  /// Cache user profile to local storage
  Future<void> _cacheProfile(User user) async {
    try {
      final jsonString = jsonEncode(user.toJson());
      await _authService.saveCachedProfile(jsonString);
      debugPrint('✅ Profile cached to local storage');
    } catch (e) {
      debugPrint('⚠️ Failed to cache profile: $e');
    }
  }

  /// Get cached profile from local storage
  Future<User> _getCachedProfile() async {
    try {
      final jsonString = await _authService.getCachedProfile();

      if (jsonString == null) {
        throw Exception('No cached profile available');
      }

      final jsonData = jsonDecode(jsonString) as Map<String, dynamic>;
      return User.fromJson(jsonData);
    } catch (e) {
      debugPrint('⚠️ Failed to load cached profile: $e');
      throw Exception('No profile available offline');
    }
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
          headers: {'Authorization': 'Bearer $token'},
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
