import 'package:flutter_test/flutter_test.dart';
import 'package:mockito/mockito.dart';
import 'package:mockito/annotations.dart';
import 'package:go_hard_app/providers/auth_provider.dart';
import 'package:go_hard_app/data/repositories/auth_repository.dart';
import 'package:go_hard_app/data/services/auth_service.dart';
import 'package:go_hard_app/data/local/services/local_database_service.dart';
import 'package:go_hard_app/data/models/auth_response.dart';

@GenerateMocks([AuthRepository, AuthService, LocalDatabaseService])
import 'auth_provider_test.mocks.dart';

void main() {
  late AuthProvider authProvider;
  late MockAuthRepository mockAuthRepository;
  late MockAuthService mockAuthService;
  late MockLocalDatabaseService mockLocalDb;

  setUp(() {
    mockAuthRepository = MockAuthRepository();
    mockAuthService = MockAuthService();
    mockLocalDb = MockLocalDatabaseService();

    // Stub the auth check methods called in constructor
    when(mockAuthService.isAuthenticated()).thenAnswer((_) async => false);
    when(mockAuthService.getUserId()).thenAnswer((_) async => null);
    when(mockAuthService.getUserName()).thenAnswer((_) async => null);
    when(mockAuthService.getUserEmail()).thenAnswer((_) async => null);

    authProvider = AuthProvider(
      mockAuthRepository,
      mockAuthService,
      mockLocalDb,
    );
  });

  group('AuthProvider - Login Tests', () {
    test('login() should succeed with valid credentials', () async {
      // Arrange
      const email = 'test@example.com';
      const password = 'password123';
      final authResponse = AuthResponse(
        token: 'fake-jwt-token',
        userId: 1,
        name: 'Test User',
        email: email,
      );

      when(mockAuthRepository.login(any)).thenAnswer((_) async => authResponse);
      when(
        mockAuthService.saveToken(
          token: anyNamed('token'),
          userId: anyNamed('userId'),
          name: anyNamed('name'),
          email: anyNamed('email'),
        ),
      ).thenAnswer((_) async => {});

      authProvider.updateEmail(email);
      authProvider.updatePassword(password);

      // Act
      final result = await authProvider.login();

      // Assert
      expect(result, true);
      expect(authProvider.isAuthenticated, true);
      expect(authProvider.currentUserId, 1);
      expect(authProvider.currentUserName, 'Test User');
      expect(authProvider.currentUserEmail, email);
      expect(authProvider.errorMessage, '');
      verify(mockAuthRepository.login(any)).called(1);
      verify(
        mockAuthService.saveToken(
          token: 'fake-jwt-token',
          userId: 1,
          name: 'Test User',
          email: email,
        ),
      ).called(1);
    });

    test('login() should fail with empty email', () async {
      // Arrange
      authProvider.updateEmail('');
      authProvider.updatePassword('password123');

      // Act
      final result = await authProvider.login();

      // Assert
      expect(result, false);
      expect(authProvider.isAuthenticated, false);
      expect(authProvider.errorMessage, 'Please enter both email and password');
      verifyNever(mockAuthRepository.login(any));
    });

    test('login() should fail with empty password', () async {
      // Arrange
      authProvider.updateEmail('test@example.com');
      authProvider.updatePassword('');

      // Act
      final result = await authProvider.login();

      // Assert
      expect(result, false);
      expect(authProvider.isAuthenticated, false);
      expect(authProvider.errorMessage, 'Please enter both email and password');
      verifyNever(mockAuthRepository.login(any));
    });

    test('login() should handle repository errors', () async {
      // Arrange
      authProvider.updateEmail('test@example.com');
      authProvider.updatePassword('password123');

      when(
        mockAuthRepository.login(any),
      ).thenThrow(Exception('Invalid credentials'));

      // Act
      final result = await authProvider.login();

      // Assert
      expect(result, false);
      expect(authProvider.isAuthenticated, false);
      expect(authProvider.errorMessage, contains('Login failed'));
      verify(mockAuthRepository.login(any)).called(1);
    });
  });

  group('AuthProvider - Signup Tests', () {
    test('signup() should succeed with valid data', () async {
      // Arrange
      const name = 'Test User';
      const email = 'test@example.com';
      const password = 'password123';
      final authResponse = AuthResponse(
        token: 'fake-jwt-token',
        userId: 1,
        name: name,
        email: email,
      );

      when(
        mockAuthRepository.signup(any),
      ).thenAnswer((_) async => authResponse);
      when(
        mockAuthService.saveToken(
          token: anyNamed('token'),
          userId: anyNamed('userId'),
          name: anyNamed('name'),
          email: anyNamed('email'),
        ),
      ).thenAnswer((_) async => {});

      authProvider.setSignupName(name);
      authProvider.setSignupEmail(email);
      authProvider.setSignupPassword(password);
      authProvider.setSignupConfirmPassword(password); // Must match password

      // Act
      final result = await authProvider.signup();

      // Assert
      expect(result, true);
      expect(authProvider.isAuthenticated, true);
      expect(authProvider.currentUserId, 1);
      verify(mockAuthRepository.signup(any)).called(1);
    });

    test('signup() should fail with short password', () async {
      // Arrange
      authProvider.updateName('Test User');
      authProvider.updateEmail('test@example.com');
      authProvider.updatePassword('123'); // Too short

      // Act
      final result = await authProvider.signup();

      // Assert
      expect(result, false);
      expect(
        authProvider.errorMessage,
        'Password must be at least 6 characters',
      );
      verifyNever(mockAuthRepository.signup(any));
    });
  });

  group('AuthProvider - Logout Tests', () {
    test('logout() should clear all auth data', () async {
      // Arrange
      when(mockAuthService.clearToken()).thenAnswer((_) async => {});

      // Act
      await authProvider.logout();

      // Assert
      expect(authProvider.isAuthenticated, false);
      expect(authProvider.currentUserId, null);
      expect(authProvider.currentUserName, null);
      expect(authProvider.currentUserEmail, null);
      verify(mockAuthService.clearToken()).called(1);
    });
  });

  group('AuthProvider - Error Handling', () {
    test('clearError() should clear error message', () {
      // Arrange
      authProvider.setError('Test error');
      expect(authProvider.errorMessage, 'Test error');

      // Act
      authProvider.clearError();

      // Assert
      expect(authProvider.errorMessage, '');
    });

    test('setError() should set error message', () {
      // Act
      authProvider.setError('Custom error');

      // Assert
      expect(authProvider.errorMessage, 'Custom error');
    });
  });
}
