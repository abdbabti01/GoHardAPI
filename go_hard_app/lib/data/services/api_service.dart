import 'package:dio/dio.dart';
import '../../core/constants/api_config.dart';
import 'auth_service.dart';

/// HTTP API service using Dio
/// Matches the ApiService.cs from MAUI app with automatic JWT token injection
class ApiService {
  late final Dio _dio;
  final AuthService _authService;

  ApiService(this._authService) {
    _dio = Dio(
      BaseOptions(
        baseUrl: ApiConfig.baseUrl,
        connectTimeout: ApiConfig.connectTimeout,
        receiveTimeout: ApiConfig.receiveTimeout,
        headers: {
          'Content-Type': 'application/json',
          'Accept': 'application/json',
        },
      ),
    );

    // Add interceptor for automatic JWT token injection
    _dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) async {
          // Inject Bearer token for all requests
          final token = await _authService.getToken();
          if (token != null && token.isNotEmpty) {
            options.headers['Authorization'] = 'Bearer $token';
          }
          return handler.next(options);
        },
        onError: (error, handler) {
          // Handle 401 Unauthorized - clear token and redirect to login
          if (error.response?.statusCode == 401) {
            _authService.clearToken();
          }
          return handler.next(error);
        },
      ),
    );
  }

  /// Generic GET request
  Future<T> get<T>(String path, {Map<String, dynamic>? queryParameters}) async {
    try {
      final response = await _dio.get<T>(
        path,
        queryParameters: queryParameters,
      );
      return response.data as T;
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  /// Generic POST request
  Future<T> post<T>(String path, {dynamic data}) async {
    try {
      final response = await _dio.post<T>(path, data: data);
      return response.data as T;
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  /// Generic PUT request
  Future<T> put<T>(String path, {dynamic data}) async {
    try {
      final response = await _dio.put<T>(path, data: data);
      return response.data as T;
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  /// Generic PATCH request
  Future<T?> patch<T>(String path, {dynamic data}) async {
    try {
      final response = await _dio.patch<T>(path, data: data);
      // Handle NoContent (204) responses
      if (response.statusCode == 204 || response.data == null) {
        return null;
      }
      return response.data as T;
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  /// Generic DELETE request
  Future<bool> delete(String path) async {
    try {
      final response = await _dio.delete(path);
      return response.statusCode == 200 || response.statusCode == 204;
    } on DioException catch (e) {
      throw _handleError(e);
    }
  }

  /// Handle Dio errors and convert to user-friendly exceptions
  Exception _handleError(DioException error) {
    if (error.type == DioExceptionType.connectionTimeout) {
      return Exception(
        'Connection timeout - please check your internet connection',
      );
    } else if (error.type == DioExceptionType.receiveTimeout) {
      return Exception('Request timeout - server took too long to respond');
    } else if (error.type == DioExceptionType.connectionError) {
      return Exception('Network error - cannot connect to server');
    } else if (error.response != null) {
      final statusCode = error.response?.statusCode;
      final data = error.response?.data;

      // Extract validation errors for 400 Bad Request
      if (statusCode == 400 && data is Map) {
        final errors = data['errors'];
        if (errors is Map && errors.isNotEmpty) {
          // Format validation errors: "FieldName: error message"
          final errorMessages = <String>[];
          errors.forEach((key, value) {
            if (value is List && value.isNotEmpty) {
              errorMessages.add('$key: ${value.first}');
            }
          });
          if (errorMessages.isNotEmpty) {
            return Exception('Validation errors:\n${errorMessages.join('\n')}');
          }
        }
      }

      final message =
          data?['message'] ??
          data?['title'] ??
          error.response?.statusMessage ??
          'Server error';

      switch (statusCode) {
        case 400:
          return Exception('Bad request: $message');
        case 401:
          return Exception('Unauthorized - please login again');
        case 403:
          return Exception('Forbidden - you don\'t have permission');
        case 404:
          return Exception('Not found: $message');
        case 500:
          return Exception('Server error: $message');
        default:
          return Exception('Error ($statusCode): $message');
      }
    } else {
      return Exception('Network error: ${error.message}');
    }
  }
}
