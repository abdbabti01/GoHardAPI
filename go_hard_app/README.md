# GoHard - Fitness Tracking App

A cross-platform mobile application for tracking workouts and fitness progress, built with Flutter and powered by a .NET Web API backend.

## Features

### Authentication
- User registration and login with JWT authentication
- Secure token storage using flutter_secure_storage
- Persistent authentication across app sessions

### Workout Sessions
- Create and manage workout sessions
- Real-time workout timer during active sessions
- View detailed session history with exercise breakdown
- Swipe-to-delete sessions
- Pull-to-refresh for latest data

### Exercise Management
- Browse exercise library with 18+ pre-defined templates
- Filter exercises by category (Chest, Back, Legs, Shoulders, Arms, Core, Cardio)
- Add exercises to active workout sessions
- Log sets with reps and weight tracking
- Mark sets as complete during workouts

### User Profile
- View user information
- Track fitness goals and progress
- Logout functionality

## Tech Stack

### Frontend (Mobile)
- **Framework**: Flutter 3.29.2
- **Language**: Dart ^3.7.2
- **State Management**: Provider pattern
- **HTTP Client**: Dio 5.4.0
- **Secure Storage**: flutter_secure_storage 9.0.0
- **Date Formatting**: intl 0.19.0

### Backend
- **API**: ASP.NET Core 8.0 Web API
- **Database**: SQL Server
- **Authentication**: JWT tokens

## Prerequisites

Before running this project, ensure you have:

- Flutter SDK 3.29.2 or higher ([Install Flutter](https://docs.flutter.dev/get-started/install))
- Dart 3.7.2 or higher (included with Flutter)
- Android Studio / Xcode for mobile development
- A running instance of the GoHardAPI backend
- For Android: Java 17+
- For iOS: macOS with Xcode 15+

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/GoHardAPI.git
cd GoHardAPI/go_hard_app
```

### 2. Install Dependencies

```bash
flutter pub get
```

### 3. Generate Mock Files (for testing)

```bash
flutter pub run build_runner build --delete-conflicting-outputs
```

### 4. Configure API Endpoints

The app automatically uses platform-specific API URLs:

- **iOS Simulator**: `http://localhost:5121/api`
- **Android Emulator**: `http://10.0.2.2:5121/api`

To use a different API URL, modify `lib/core/constants/api_config.dart`:

```dart
class ApiConfig {
  static String get baseUrl {
    if (Platform.isIOS) {
      return 'http://your-api-url:5121/api';
    } else if (Platform.isAndroid) {
      return 'http://your-api-url:5121/api';
    }
    return 'http://localhost:5121/api';
  }
}
```

### 5. Run the App

```bash
# Run on connected device/emulator
flutter run

# Run on specific device
flutter devices
flutter run -d <device-id>

# Run in release mode
flutter run --release
```

## Project Structure

```
go_hard_app/
├── lib/
│   ├── main.dart                    # App entry point
│   ├── app.dart                     # Root app widget with providers
│   │
│   ├── core/
│   │   ├── constants/
│   │   │   ├── colors.dart          # Color palette
│   │   │   ├── api_config.dart      # API configuration
│   │   │   └── strings.dart         # String constants
│   │   ├── theme/
│   │   │   └── app_theme.dart       # Light/dark theme
│   │   └── utils/
│   │       └── category_helpers.dart # Category utilities
│   │
│   ├── data/
│   │   ├── models/                   # Data models (8 files)
│   │   ├── repositories/             # Repository layer
│   │   └── services/                 # API and auth services
│   │
│   ├── providers/                    # State management (7 providers)
│   │   ├── auth_provider.dart
│   │   ├── sessions_provider.dart
│   │   ├── active_workout_provider.dart
│   │   ├── exercises_provider.dart
│   │   ├── exercise_detail_provider.dart
│   │   ├── log_sets_provider.dart
│   │   └── profile_provider.dart
│   │
│   ├── ui/
│   │   ├── screens/                  # 10 app screens
│   │   │   ├── auth/                 # Login, Signup
│   │   │   ├── sessions/             # Sessions list, detail, active workout
│   │   │   ├── exercises/            # Exercise library, detail, logging
│   │   │   ├── profile/              # User profile
│   │   │   └── main_screen.dart      # Bottom navigation wrapper
│   │   │
│   │   └── widgets/                  # Reusable widgets
│   │       ├── common/
│   │       ├── sessions/
│   │       └── exercises/
│   │
│   └── routes/
│       ├── app_router.dart           # Route configuration
│       └── route_names.dart          # Route constants
│
└── test/
    ├── providers/                     # Unit tests for providers
    └── ui/screens/                    # Widget tests for screens
```

## Testing

### Run All Tests

```bash
flutter test
```

### Run Specific Test File

```bash
flutter test test/providers/auth_provider_test.dart
```

### Test Coverage

```bash
flutter test --coverage
```

### Current Test Suite

- **Unit Tests**: 9+ tests for AuthProvider
- **Widget Tests**: 7+ tests for LoginScreen
- All tests use mockito for dependency mocking

## Building for Production

### Android APK (for sideloading)

```bash
flutter build apk --release
```

Output: `build/app/outputs/flutter-apk/app-release.apk`

### Android App Bundle (for Google Play)

```bash
flutter build appbundle --release
```

Output: `build/app/outputs/bundle/release/app-release.aab`

### iOS (unsigned, for testing)

```bash
flutter build ios --release --no-codesign
```

For signed iOS builds, configure code signing in Xcode.

## CI/CD Pipeline

The project uses GitHub Actions for automated testing and builds.

### Workflow Triggers
- Push to `main` or `release/*` branches
- Pull requests to `main`
- Manual workflow dispatch

### Pipeline Jobs

1. **Test**: Format check, static analysis, unit & widget tests
2. **Build Android APK**: Release APK for sideloading
3. **Build Android AAB**: App bundle for Google Play Store
4. **Build iOS**: Unsigned IPA for testing
5. **Build Summary**: Aggregate results from all jobs

### Artifacts

Build artifacts are stored for 30 days and can be downloaded from the Actions tab.

## Development Guidelines

### State Management Pattern

This app uses the Provider pattern with ChangeNotifier:

```dart
class ExampleProvider extends ChangeNotifier {
  // Private state
  List<Item> _items = [];
  bool _isLoading = false;

  // Public getters
  List<Item> get items => _items;
  bool get isLoading => _isLoading;

  // Methods that modify state
  Future<void> loadItems() async {
    _isLoading = true;
    notifyListeners(); // Update UI

    _items = await _repository.getItems();
    _isLoading = false;
    notifyListeners(); // Update UI
  }
}
```

### Adding a New Screen

1. Create screen file in `lib/ui/screens/`
2. Define route in `lib/routes/route_names.dart`
3. Add route to `lib/routes/app_router.dart`
4. Create provider if needed
5. Add provider to MultiProvider in `lib/app.dart`

### API Integration

All API calls go through repositories:

```dart
class ExampleRepository {
  final ApiService _apiService;

  Future<List<Item>> getItems() async {
    final response = await _apiService.get('/items');
    return (response.data as List)
        .map((json) => Item.fromJson(json))
        .toList();
  }
}
```

JWT tokens are automatically injected via Dio interceptors.

## Troubleshooting

### Issue: API Connection Failed

**Solution**: Ensure the backend API is running and accessible:
- Check API URL in `lib/core/constants/api_config.dart`
- For Android emulator, use `10.0.2.2` instead of `localhost`
- Verify backend is running: `cd GoHardAPI && dotnet run`

### Issue: Build Failed on iOS

**Solution**:
1. Run `pod install` in the `ios/` directory
2. Clean build: `flutter clean && flutter pub get`
3. Update CocoaPods: `sudo gem install cocoapods`

### Issue: Tests Failing

**Solution**:
1. Regenerate mocks: `flutter pub run build_runner build --delete-conflicting-outputs`
2. Ensure all dependencies are installed: `flutter pub get`
3. Check for missing stubs in mock setup

### Issue: Hot Reload Not Working

**Solution**:
1. Try hot restart instead (Shift+R in terminal)
2. Stop and restart the app
3. Run `flutter clean` and restart

## Contributing

1. Create a feature branch: `git checkout -b feature/my-feature`
2. Make your changes
3. Write tests for new functionality
4. Ensure all tests pass: `flutter test`
5. Format code: `flutter format .`
6. Run static analysis: `flutter analyze`
7. Commit changes: `git commit -m "Add my feature"`
8. Push to branch: `git push origin feature/my-feature`
9. Create a pull request

## License

This project is licensed under the MIT License.

## Support

For issues and questions:
- Create an issue in the GitHub repository
- Check existing documentation in `/docs`
- Review API documentation at `/swagger` when backend is running

## Roadmap

- [ ] Add workout analytics and progress charts
- [ ] Implement social features (share workouts)
- [ ] Add exercise video demonstrations
- [ ] Offline mode with local database sync
- [ ] Wearable device integration
- [ ] Custom exercise creation
- [ ] Workout plans and programs
