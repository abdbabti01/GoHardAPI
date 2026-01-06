# GoHard - Fitness Tracking App

A cross-platform mobile application for tracking workouts and fitness progress, built with Flutter and powered by a .NET Web API backend. Features offline-first architecture, social workout sharing, analytics, and workout templates.

## 🎯 Features Overview

### ✅ Phase 1: Offline-First Architecture
- **Offline Mode**: Full app functionality without internet connection
- **Local Database**: Isar database for local data persistence
- **Background Sync**: Automatic sync when connection restored
- **Optimistic Updates**: Instant UI updates with background server sync
- **Connectivity Detection**: Real-time online/offline status monitoring
- **Visual Feedback**: Offline banner and sync status indicators

### ✅ Phase 2: Analytics & Progress Tracking
- **Calendar Heatmap**: Visual workout activity calendar with color intensity
- **Streak Counter**: Current and longest workout streaks tracking
- **Weekly Progress**: Sessions completed this week with goal tracking
- **Exercise Analytics**: Most trained muscles and favorite exercises
- **Quick Actions**: Fast access to start workout and view history
- **Interactive Charts**: Tap dates to view workout details

### ✅ Phase 3: Social & Templates
- **Workout Templates**: Create recurring workout schedules (daily/weekly/custom)
- **Community Sharing**: Browse and discover workouts shared by other users
- **Social Features**: Like, save, and rate workouts and templates
- **Template Scheduling**: Auto-suggest workouts based on recurrence patterns
- **Community Library**: Access pre-made templates from other users
- **Usage Tracking**: Monitor template usage and popularity

### 🔐 Authentication
- User registration and login with JWT authentication
- Secure token storage using flutter_secure_storage
- Persistent authentication across app sessions
- Auto-refresh token handling

### 💪 Workout Sessions
- Create and manage workout sessions
- Real-time workout timer during active sessions
- View detailed session history with exercise breakdown
- Swipe-to-delete sessions
- Pull-to-refresh for latest data
- Offline session creation with background sync

### 🏋️ Exercise Management
- Browse exercise library with 18+ pre-defined templates
- Filter exercises by category (Chest, Back, Legs, Shoulders, Arms, Core, Cardio)
- Filter by muscle group and equipment
- Add exercises to active workout sessions
- Log sets with reps and weight tracking
- Mark sets as complete during workouts
- Custom exercise creation

### 👤 User Profile
- View user information
- Track fitness goals and progress
- Update profile settings
- Logout functionality

## 🛠 Tech Stack

### Frontend (Mobile)
- **Framework**: Flutter 3.29.2
- **Language**: Dart ^3.7.2
- **State Management**: Provider pattern (ChangeNotifier)
- **HTTP Client**: Dio 5.4.0
- **Secure Storage**: flutter_secure_storage 9.0.0
- **Local Database**: Isar 4.0.3 (NoSQL, offline-first)
- **Connectivity**: connectivity_plus 6.1.2
- **Charts**: fl_chart 0.70.2
- **Date Formatting**: intl 0.19.0
- **Testing**: mockito 5.4.4, build_runner 2.4.14

### Backend
- **API**: ASP.NET Core 8.0 Web API
- **Database**: SQL Server
- **Authentication**: JWT tokens
- **ORM**: Entity Framework Core 8.0

### Architecture Pattern
- **MVVM-like**: Providers act as ViewModels
- **Repository Pattern**: Data layer abstraction
- **Dependency Injection**: Provider-based DI
- **Offline-First**: Local cache with background sync

## 📋 Prerequisites

Before running this project, ensure you have:

- Flutter SDK 3.29.2 or higher ([Install Flutter](https://docs.flutter.dev/get-started/install))
- Dart 3.7.2 or higher (included with Flutter)
- Android Studio / Xcode for mobile development
- A running instance of the GoHardAPI backend
- For Android: Java 17+
- For iOS: macOS with Xcode 15+

## 🚀 Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/GoHardAPI.git
cd GoHardAPI/go_hard_app
```

### 2. Install Dependencies

```bash
flutter pub get
```

### 3. Generate Code (Isar schemas & mocks)

```bash
# Generate Isar database schemas and test mocks
flutter pub run build_runner build --delete-conflicting-outputs
```

### 4. Configure API Endpoints

The app automatically uses platform-specific API URLs defined in `lib/core/constants/api_config.dart`:

- **iOS Simulator**: `http://localhost:5121/api`
- **Android Emulator**: `http://10.0.2.2:5121/api`
- **Windows**: `http://localhost:5121/api`

API Endpoints configured:
```dart
static const String sessions = '$baseUrl/sessions';
static const String exercises = '$baseUrl/exercises';
static const String exerciseTemplates = '$baseUrl/exercisetemplates';
static const String users = '$baseUrl/users';
static const String sharedWorkouts = '$baseUrl/sharedworkouts';
static const String workoutTemplates = '$baseUrl/workouttemplates';
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

## 📁 Project Structure

```
go_hard_app/
├── lib/
│   ├── main.dart                           # App entry point with Isar initialization
│   ├── app.dart                            # Root widget with all providers
│   │
│   ├── core/                               # Core utilities and configuration
│   │   ├── constants/
│   │   │   ├── colors.dart                 # App color palette
│   │   │   ├── api_config.dart             # API base URLs and endpoints
│   │   │   └── strings.dart                # String constants
│   │   ├── theme/
│   │   │   └── app_theme.dart              # Light/dark theme configuration
│   │   └── utils/
│   │       └── category_helpers.dart       # Category icon/color utilities
│   │
│   ├── data/                               # Data layer (models, repositories, services)
│   │   ├── models/                         # Data models with JSON serialization
│   │   │   ├── user.dart                   # User model
│   │   │   ├── session.dart                # Workout session model (Isar schema)
│   │   │   ├── exercise.dart               # Exercise model (Isar schema)
│   │   │   ├── exercise_set.dart           # Exercise set model (Isar schema)
│   │   │   ├── exercise_template.dart      # Exercise template model (Isar schema)
│   │   │   ├── shared_workout.dart         # Shared workout model (Isar schema)
│   │   │   ├── workout_template.dart       # Workout template model (Isar schema)
│   │   │   └── workout_suggestion.dart     # AI workout suggestion model
│   │   │
│   │   ├── repositories/                   # Repository pattern (offline-first)
│   │   │   ├── session_repository.dart     # Sessions CRUD + offline cache
│   │   │   ├── exercise_repository.dart    # Exercises CRUD + offline cache
│   │   │   ├── shared_workout_repository.dart  # Community workouts
│   │   │   └── workout_template_repository.dart # Templates CRUD
│   │   │
│   │   ├── services/                       # Core services
│   │   │   ├── api_service.dart            # HTTP client with Dio + JWT interceptor
│   │   │   └── auth_service.dart           # Token storage and auth state
│   │   │
│   │   └── local/                          # Local database layer
│   │       └── services/
│   │           ├── local_database_service.dart  # Isar database wrapper
│   │           └── connectivity_service.dart    # Network connectivity monitor
│   │
│   ├── providers/                          # State management (ViewModels)
│   │   ├── auth_provider.dart              # Authentication state
│   │   ├── sessions_provider.dart          # Sessions list + sync logic
│   │   ├── active_workout_provider.dart    # Active workout timer + state
│   │   ├── exercises_provider.dart         # Exercise library state
│   │   ├── exercise_detail_provider.dart   # Single exercise detail
│   │   ├── log_sets_provider.dart          # Exercise set logging
│   │   ├── profile_provider.dart           # User profile state
│   │   ├── analytics_provider.dart         # Analytics calculations
│   │   ├── shared_workout_provider.dart    # Community workouts state
│   │   └── workout_template_provider.dart  # Templates state
│   │
│   ├── ui/                                 # Presentation layer
│   │   ├── screens/                        # All app screens
│   │   │   ├── auth/                       # Authentication screens
│   │   │   │   ├── login_screen.dart       # Login UI
│   │   │   │   └── signup_screen.dart      # Registration UI
│   │   │   │
│   │   │   ├── sessions/                   # Workout sessions
│   │   │   │   ├── sessions_screen.dart    # Sessions list (main screen)
│   │   │   │   ├── session_detail_screen.dart  # Single session view
│   │   │   │   └── active_workout_screen.dart  # Live workout tracker
│   │   │   │
│   │   │   ├── exercises/                  # Exercise library
│   │   │   │   ├── exercises_screen.dart   # Exercise list with filters
│   │   │   │   ├── exercise_detail_screen.dart  # Exercise details
│   │   │   │   └── log_sets_screen.dart    # Set logging during workout
│   │   │   │
│   │   │   ├── profile/                    # User profile
│   │   │   │   └── profile_screen.dart     # User info and settings
│   │   │   │
│   │   │   ├── analytics/                  # Analytics & Progress (Phase 2)
│   │   │   │   └── analytics_screen.dart   # Heatmap, streaks, charts
│   │   │   │
│   │   │   ├── community/                  # Social features (Phase 3)
│   │   │   │   └── community_screen.dart   # Shared workouts (3 tabs)
│   │   │   │
│   │   │   ├── templates/                  # Workout templates (Phase 3)
│   │   │   │   ├── templates_screen.dart   # Templates list (2 tabs)
│   │   │   │   └── template_form_dialog.dart  # Create/edit template form
│   │   │   │
│   │   │   └── main_screen.dart            # Bottom nav wrapper (legacy)
│   │   │
│   │   └── widgets/                        # Reusable UI components
│   │       ├── common/
│   │       │   ├── offline_banner.dart     # Offline mode indicator
│   │       │   └── loading_indicator.dart  # Loading spinner
│   │       │
│   │       ├── sessions/
│   │       │   ├── session_card.dart       # Session list item
│   │       │   ├── workout_timer.dart      # Active workout timer
│   │       │   └── exercise_list_item.dart # Exercise in session
│   │       │
│   │       ├── exercises/
│   │       │   ├── exercise_card.dart      # Exercise library item
│   │       │   ├── set_row.dart            # Set input row
│   │       │   └── category_chip.dart      # Category filter chip
│   │       │
│   │       └── analytics/
│   │           ├── calendar_heatmap.dart   # Activity calendar
│   │           ├── streak_counter.dart     # Streak display
│   │           └── stat_card.dart          # Metric card
│   │
│   └── routes/                             # Navigation configuration
│       ├── app_router.dart                 # Route definitions and guards
│       └── route_names.dart                # Route string constants
│
├── test/                                   # Unit and widget tests
│   ├── providers/                          # Provider unit tests
│   │   ├── auth_provider_test.dart         # Auth tests (9+ tests)
│   │   └── *.mocks.dart                    # Generated mocks
│   │
│   └── ui/screens/                         # Widget tests
│       └── auth/
│           ├── login_screen_test.dart      # Login UI tests (7+ tests)
│           └── *.mocks.dart                # Generated mocks
│
├── assets/                                 # Static assets (if any)
│
├── android/                                # Android platform code
├── ios/                                    # iOS platform code
├── windows/                                # Windows platform code
│
├── pubspec.yaml                            # Dependencies and metadata
└── README.md                               # This file
```

## 🗂 Key Files Explained

### Entry Point & App Setup

#### `lib/main.dart`
- App entry point
- Initializes Isar local database
- Runs the App widget

#### `lib/app.dart`
- Root widget wrapping MaterialApp
- Sets up MultiProvider with all 10 providers
- Configures routing and theme
- Dependency injection setup

### Data Layer

#### `lib/data/models/*.dart`
Models with annotations for JSON serialization and Isar schemas:
- `@collection` - Marks Isar database tables
- `@JsonSerializable()` - Generates JSON converters
- Models include: Session, Exercise, ExerciseSet, ExerciseTemplate, SharedWorkout, WorkoutTemplate

#### `lib/data/repositories/*.dart`
**Offline-First Repository Pattern**:
```dart
class SessionRepository {
  // 1. Load from local cache FIRST
  Future<List<Session>> getSessions() async {
    final cached = await _localDb.getAllSessions();

    // 2. Return cache immediately (offline support)
    if (_connectivity.isOffline) return cached;

    // 3. Background sync from server
    _syncFromServer().catchError(...);

    // 4. Return cached data (fast UI)
    return cached;
  }
}
```

Key repositories:
- **SessionRepository**: Sessions CRUD with offline queue
- **ExerciseRepository**: Exercise templates with local cache
- **SharedWorkoutRepository**: Community workouts with social features
- **WorkoutTemplateRepository**: Templates with scheduling logic

#### `lib/data/services/api_service.dart`
- Dio HTTP client wrapper
- JWT token auto-injection via interceptors
- Error handling and response parsing
- Request/response logging

#### `lib/data/local/services/local_database_service.dart`
- Isar database wrapper
- Provides CRUD operations for all collections
- Handles database migrations
- Query builder wrapper

#### `lib/data/local/services/connectivity_service.dart`
- Monitors network connectivity status
- Provides isOnline stream
- Used by repositories for offline decisions

### Providers (State Management)

All providers extend `ChangeNotifier` and follow this pattern:

```dart
class ExampleProvider extends ChangeNotifier {
  // Dependencies
  final ExampleRepository _repository;

  // Private state
  List<Item> _items = [];
  bool _isLoading = false;
  String? _errorMessage;

  // Public getters
  List<Item> get items => _items;
  bool get isLoading => _isLoading;
  String? get errorMessage => _errorMessage;

  // Actions that modify state
  Future<void> loadItems() async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners(); // Update UI

    try {
      _items = await _repository.getItems();
    } catch (e) {
      _errorMessage = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners(); // Update UI
    }
  }
}
```

**Provider List**:
1. **AuthProvider**: Login, signup, logout, token management
2. **SessionsProvider**: Session list, CRUD, sync queue, offline handling
3. **ActiveWorkoutProvider**: Workout timer, pause/resume, exercise tracking
4. **ExercisesProvider**: Exercise library, filtering, search
5. **ExerciseDetailProvider**: Single exercise details
6. **LogSetsProvider**: Set logging during workouts
7. **ProfileProvider**: User profile, settings
8. **AnalyticsProvider**: Stats calculation, heatmap data, streaks
9. **SharedWorkoutProvider**: Community workouts, like/save, filters
10. **WorkoutTemplateProvider**: Templates CRUD, scheduling, rating

### UI Layer

#### Screens Organization

**auth/** - No authentication required
- Login and signup screens
- Form validation
- Error handling

**sessions/** - Main workout screens
- **sessions_screen.dart**: List all sessions, weekly progress, navigation
- **session_detail_screen.dart**: View single session with exercises
- **active_workout_screen.dart**: Live workout tracker with timer

**exercises/** - Exercise library
- **exercises_screen.dart**: Browsable library with category/muscle filters
- **exercise_detail_screen.dart**: Exercise info, sets, reps
- **log_sets_screen.dart**: Log sets during active workout

**analytics/** - Phase 2
- **analytics_screen.dart**: Calendar heatmap, streak counter, weekly progress, quick actions

**community/** - Phase 3 social features
- **community_screen.dart**: 3 tabs (Discover, Saved, My Shares), like/save buttons, filters

**templates/** - Phase 3 templates
- **templates_screen.dart**: 2 tabs (My Templates, Community), toggle active
- **template_form_dialog.dart**: Full form for create/edit with recurrence patterns

#### Widgets Organization

**common/** - Shared across app
- OfflineBanner: Shows when offline
- LoadingIndicator: Standard spinner

**sessions/** - Session-specific widgets
- SessionCard: List item for sessions
- WorkoutTimer: Live timer component

**analytics/** - Analytics widgets
- CalendarHeatmap: 30-day activity grid
- StreakCounter: Current/longest streak display
- StatCard: Metric display card

### Routing

#### `lib/routes/route_names.dart`
Route string constants:
```dart
class RouteNames {
  static const String login = '/login';
  static const String sessions = '/sessions';
  static const String analytics = '/analytics';
  static const String community = '/community';
  static const String templates = '/templates';
  // ... more routes
}
```

#### `lib/routes/app_router.dart`
- Named route configuration
- Route guards for authentication
- Unknown route handling

## 🔄 Offline-First Architecture

### How it Works

1. **Data Flow**:
   ```
   User Action → Provider → Repository → Check Connectivity
                                        ↓
                           Offline? → Local DB → Return cached
                                        ↓
                           Online? → Local DB (instant) + Background API call
   ```

2. **Write Operations**:
   ```
   Create/Update/Delete → Save to Local DB
                       ↓
                       Queue for sync
                       ↓
                       Background sync when online
                       ↓
                       Update with server IDs
   ```

3. **Optimistic Updates**:
   ```dart
   // Update UI immediately
   _sessions.add(newSession);
   notifyListeners();

   // Sync in background (with rollback on error)
   try {
     await _repository.createSession(newSession);
   } catch (e) {
     _sessions.remove(newSession); // Rollback
     notifyListeners();
   }
   ```

### Sync Strategy

- **Background Sync**: Auto-sync every 30 seconds when online
- **Manual Sync**: Pull-to-refresh on screens
- **Sync Queue**: Failed operations retry automatically
- **Conflict Resolution**: Server data wins (last-write-wins)

## 📊 Analytics Features

### Calendar Heatmap
- 30-day workout activity visualization
- Color intensity based on session count (0-4+ sessions)
- Tap dates to view sessions
- Scroll through months

### Streak Counter
- Current streak (consecutive days with workouts)
- Longest streak ever achieved
- Motivational badges

### Weekly Progress
- Sessions completed this week
- Goal tracking (e.g., 4/5 sessions)
- Progress bar visualization

### Exercise Analytics
- Most trained muscle groups (pie chart)
- Favorite exercises (bar chart)
- Total volume lifted

## 🏋️ Template System

### Recurrence Patterns

1. **Daily**: Every single day
2. **Weekly**: Specific days (Mon, Wed, Fri)
3. **Custom**: Every X days (e.g., every 3 days)

### Template Features

- **Scheduling**: Auto-suggest based on recurrence
- **Active/Inactive**: Toggle scheduling on/off
- **Usage Tracking**: Count how many times used
- **Rating System**: Community templates rated 1-5 stars
- **Category**: Strength, Cardio, HIIT, etc.

### Example Template

```json
{
  "name": "Push Day",
  "recurrencePattern": "weekly",
  "daysOfWeek": "1,4",  // Monday & Thursday
  "estimatedDuration": 60,
  "category": "Strength",
  "exercisesJson": "[{\"name\":\"Bench Press\",\"sets\":5,\"reps\":5}]",
  "isActive": true
}
```

## 🌍 Community Features

### Social Interactions

- **Like**: Heart button (optimistic update)
- **Save**: Bookmark for later
- **Use**: Create session from shared workout
- **Delete**: Remove your own shares

### Discovery

- **Filters**: Category (Strength, Cardio, HIIT, etc.)
- **Filters**: Difficulty (Beginner, Intermediate, Advanced)
- **Tabs**: Discover, Saved, My Shares
- **Sorting**: Most recent first

### Sharing Flow (Future)

```
Complete Workout → Share Button → Choose visibility → Post to Community
                                                     ↓
                                    Appears in Discover feed for all users
```

## 🧪 Testing

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
- Mock generation: `flutter pub run build_runner build`

### Test Structure

```dart
void main() {
  late AuthProvider authProvider;
  late MockAuthService mockAuthService;

  setUp(() {
    mockAuthService = MockAuthService();
    authProvider = AuthProvider(mockAuthService);
  });

  test('login success updates isAuthenticated', () async {
    when(mockAuthService.login(any, any))
        .thenAnswer((_) async => true);

    await authProvider.login('test@test.com', 'password');

    expect(authProvider.isAuthenticated, true);
    expect(authProvider.errorMessage, null);
  });
}
```

## 📦 Building for Production

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

## 🔧 Development Guidelines

### Adding a New Feature

1. **Create Model** (if needed):
   ```dart
   @JsonSerializable()
   @collection
   class MyModel {
     Id id = Isar.autoIncrement;
     String name;
     // ... fields
   }
   ```

2. **Create Repository**:
   ```dart
   class MyRepository {
     final ApiService _api;
     final LocalDatabaseService _localDb;

     Future<List<MyModel>> getItems() async {
       // Offline-first logic
     }
   }
   ```

3. **Create Provider**:
   ```dart
   class MyProvider extends ChangeNotifier {
     final MyRepository _repository;

     List<MyModel> _items = [];
     List<MyModel> get items => _items;

     Future<void> loadItems() async {
       _items = await _repository.getItems();
       notifyListeners();
     }
   }
   ```

4. **Create Screen**:
   ```dart
   class MyScreen extends StatelessWidget {
     @override
     Widget build(BuildContext context) {
       final provider = context.watch<MyProvider>();
       return Scaffold(...);
     }
   }
   ```

5. **Add Route** in `route_names.dart` and `app_router.dart`

6. **Register Provider** in `app.dart`

### Code Style

- Use `flutter format .` before committing
- Run `flutter analyze` to catch issues
- Follow Effective Dart guidelines
- Write descriptive commit messages

### State Management Best Practices

- Keep providers focused (single responsibility)
- Use private setters, public getters
- Always call `notifyListeners()` after state changes
- Handle loading and error states
- Use `context.read()` for actions, `context.watch()` for rebuilds

## 🐛 Troubleshooting

### Issue: API Connection Failed

**Solution**: Ensure the backend API is running and accessible:
- Check API URL in `lib/core/constants/api_config.dart`
- For Android emulator, use `10.0.2.2` instead of `localhost`
- For iOS simulator, use `localhost`
- Verify backend is running: `cd GoHardAPI && dotnet run`

### Issue: Isar Build Failed

**Solution**:
```bash
flutter pub run build_runner build --delete-conflicting-outputs
```

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
1. Try hot restart instead (Shift+R in terminal or `r` in CLI)
2. Stop and restart the app
3. Run `flutter clean` and restart

### Issue: Offline Mode Not Working

**Solution**:
1. Check connectivity_plus permissions in AndroidManifest.xml / Info.plist
2. Verify LocalDatabaseService is initialized in main.dart
3. Check that repositories use connectivity checks

## 🚀 CI/CD Pipeline

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

## 🤝 Contributing

1. Create a feature branch: `git checkout -b feature/my-feature`
2. Make your changes following code style guidelines
3. Write tests for new functionality
4. Ensure all tests pass: `flutter test`
5. Format code: `flutter format .`
6. Run static analysis: `flutter analyze`
7. Commit changes: `git commit -m "Add my feature"`
8. Push to branch: `git push origin feature/my-feature`
9. Create a pull request

## 📄 License

This project is licensed under the MIT License.

## 📞 Support

For issues and questions:
- Create an issue in the GitHub repository
- Check existing documentation in this README
- Review API documentation at `http://localhost:5121/swagger` when backend is running

## 🗺 Roadmap

### ✅ Completed
- [x] Authentication with JWT
- [x] Workout session tracking
- [x] Exercise library
- [x] Active workout timer
- [x] Offline-first architecture (Phase 1)
- [x] Analytics and progress tracking (Phase 2)
- [x] Workout templates with scheduling (Phase 3)
- [x] Community workout sharing (Phase 3)

### 🔜 Upcoming
- [ ] Share workout feature (post sessions to community)
- [ ] Exercise video demonstrations
- [ ] Wearable device integration (smartwatch sync)
- [ ] AI workout suggestions based on history
- [ ] Custom exercise creation with images
- [ ] Workout programs (multi-week plans)
- [ ] Social following and friends
- [ ] Achievements and badges
- [ ] Export workout data (CSV/PDF)
- [ ] Multi-language support

## 📚 Additional Resources

- [Flutter Documentation](https://docs.flutter.dev/)
- [Provider Package](https://pub.dev/packages/provider)
- [Isar Database](https://isar.dev/)
- [Dio HTTP Client](https://pub.dev/packages/dio)
- [FL Chart](https://pub.dev/packages/fl_chart)
- Backend API: See `GoHardAPI/README.md` or `CLAUDE.md`
