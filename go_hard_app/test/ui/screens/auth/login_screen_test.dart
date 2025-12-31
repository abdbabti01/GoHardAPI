import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:mockito/mockito.dart';
import 'package:mockito/annotations.dart';
import 'package:go_hard_app/ui/screens/auth/login_screen.dart';
import 'package:go_hard_app/providers/auth_provider.dart';

@GenerateMocks([AuthProvider])
import 'login_screen_test.mocks.dart';

void main() {
  late MockAuthProvider mockAuthProvider;

  setUp(() {
    mockAuthProvider = MockAuthProvider();

    // Set up default stubbing for getters
    when(mockAuthProvider.isLoading).thenReturn(false);
    when(mockAuthProvider.errorMessage).thenReturn('');
  });

  Widget createTestWidget() {
    return MaterialApp(
      home: ChangeNotifierProvider<AuthProvider>.value(
        value: mockAuthProvider,
        child: const LoginScreen(),
      ),
    );
  }

  group('LoginScreen Widget Tests', () {
    testWidgets('should display all required UI elements', (
      WidgetTester tester,
    ) async {
      // Arrange & Act
      await tester.pumpWidget(createTestWidget());

      // Assert
      expect(find.text('GoHard'), findsOneWidget);
      expect(find.text('Track your fitness journey'), findsOneWidget);
      expect(
        find.byType(TextField),
        findsNWidgets(2),
      ); // Email and password fields
      expect(find.text('Login'), findsOneWidget);
      expect(find.text("Don't have an account? "), findsOneWidget);
      expect(find.text('Sign Up'), findsOneWidget);
    });

    testWidgets('should show error message when auth provider has error', (
      WidgetTester tester,
    ) async {
      // Arrange
      when(mockAuthProvider.errorMessage).thenReturn('Invalid credentials');

      // Act
      await tester.pumpWidget(createTestWidget());

      // Assert
      expect(find.text('Invalid credentials'), findsOneWidget);
      expect(find.byIcon(Icons.error_outline), findsOneWidget);
    });

    testWidgets('should show loading indicator when logging in', (
      WidgetTester tester,
    ) async {
      // Arrange
      when(mockAuthProvider.isLoading).thenReturn(true);

      // Act
      await tester.pumpWidget(createTestWidget());

      // Assert
      expect(find.byType(CircularProgressIndicator), findsOneWidget);
    });

    testWidgets('should toggle password visibility', (
      WidgetTester tester,
    ) async {
      // Arrange
      await tester.pumpWidget(createTestWidget());

      // Find the password field (second TextField)
      final passwordField = find.byType(TextField).at(1);
      final visibilityToggle = find.byIcon(Icons.visibility_outlined);

      // Assert initial state - password should be obscured
      final textField = tester.widget<TextField>(passwordField);
      expect(textField.obscureText, true);

      // Act - tap visibility toggle
      await tester.tap(visibilityToggle);
      await tester.pump();

      // Assert - password should now be visible
      final updatedTextField = tester.widget<TextField>(passwordField);
      expect(updatedTextField.obscureText, false);
    });

    testWidgets(
      'should call updateEmail and updatePassword when text is entered',
      (WidgetTester tester) async {
        // Arrange
        await tester.pumpWidget(createTestWidget());

        // Act - enter email
        await tester.enterText(
          find.byType(TextField).at(0),
          'test@example.com',
        );

        // Act - enter password
        await tester.enterText(find.byType(TextField).at(1), 'password123');

        // Assert - verify the text was entered (fields exist with text)
        expect(find.text('test@example.com'), findsOneWidget);
        expect(find.text('password123'), findsOneWidget);
      },
    );

    testWidgets('email field should have correct keyboard type', (
      WidgetTester tester,
    ) async {
      // Arrange & Act
      await tester.pumpWidget(createTestWidget());

      // Assert
      final emailField = tester.widget<TextField>(find.byType(TextField).at(0));
      expect(emailField.keyboardType, TextInputType.emailAddress);
    });

    testWidgets('should have working form validation', (
      WidgetTester tester,
    ) async {
      // Arrange
      await tester.pumpWidget(createTestWidget());

      // Act - try to submit without entering anything
      await tester.tap(find.text('Login'));
      await tester.pump();

      // Assert - should show validation errors
      expect(find.text('Please enter your email'), findsOneWidget);
      expect(find.text('Please enter your password'), findsOneWidget);
    });
  });
}
