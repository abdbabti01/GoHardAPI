# GoHard Fitness App - Complete Documentation

> **A modern fitness tracking application with full offline support**
> Backend: ASP.NET Core 8.0 | Frontend: Flutter | Database: SQL Server + Isar

---

## Table of Contents

1. [Project Overview](#project-overview)
2. [System Architecture](#system-architecture)
3. [Technology Stack](#technology-stack)
4. [API Documentation](#api-documentation)
5. [Mobile App Documentation](#mobile-app-documentation)
6. [Database Schema](#database-schema)
7. [Offline Mode & Sync](#offline-mode--sync)
8. [Development Guide](#development-guide)
9. [Deployment](#deployment)
10. [Testing](#testing)
11. [Troubleshooting](#troubleshooting)

---

## Project Overview

### What is GoHard?

GoHard is a comprehensive fitness tracking application that allows users to:
- Create and track workout sessions (online and offline)
- Log exercises with sets, reps, and weights
- Browse a library of exercise templates
- Automatically sync data when connectivity is restored
- Track workout history and progress

### Key Features

- ✅ **Full Offline Mode** - Create workouts, log exercises, everything works offline
- ✅ **Automatic Sync** - Data syncs automatically when online
- ✅ **Real-time Timer** - Live workout timer with pause/resume
- ✅ **Exercise Library** - Pre-loaded templates + custom exercises
- ✅ **Cross-Platform** - iOS, Android, Web, Desktop (Flutter)
- ✅ **Secure** - JWT authentication with user data isolation
- ✅ **Fast** - Optimized with local caching for instant responses

---

## System Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         Flutter App                              │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │                    UI Layer (Screens)                      │  │
│  └────────────────────────┬───────────────────────────────────┘  │
│  ┌────────────────────────▼───────────────────────────────────┐  │
│  │              State Management (Providers)                   │  │
│  │   • AuthProvider  • SessionsProvider  • ActiveWorkout      │  │
│  └────────────────────────┬───────────────────────────────────┘  │
│  ┌────────────────────────▼───────────────────────────────────┐  │
│  │                 Repository Layer                            │  │
│  │   • SessionRepository  • ExerciseRepository                │  │
│  └────────┬────────────────┬──────────────────────────────────┘  │
│  ┌────────▼───────┐  ┌─────▼──────────┐                         │
│  │  API Service   │  │  Local DB      │                         │
│  │  (HTTP/REST)   │  │  (Isar NoSQL)  │                         │
│  └────────┬───────┘  └────────────────┘                         │
└───────────┼──────────────────────────────────────────────────────┘
            │
            │ HTTPS/REST API
            │
┌───────────▼──────────────────────────────────────────────────────┐
│                     ASP.NET Core API                             │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │                    Controllers                            │   │
│  │  • UsersController  • SessionsController                 │   │
│  │  • ExerciseTemplatesController                           │   │
│  └────────────────────────┬─────────────────────────────────┘   │
│  ┌────────────────────────▼─────────────────────────────────┐   │
│  │              Entity Framework Core                        │   │
│  │                 (ORM - DbContext)                         │   │
│  └────────────────────────┬─────────────────────────────────┘   │
│  ┌────────────────────────▼─────────────────────────────────┐   │
│  │                  SQL Server Database                      │   │
│  │    • Users  • Sessions  • Exercises  • ExerciseSets      │   │
│  └──────────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────────┘
```

### Data Flow

#### Online Mode
```
User Action → Provider → Repository → Check Local Cache
                              ↓
                         API Service → REST API → SQL Server
                              ↓
                        Update Cache → Update UI
```

#### Offline Mode
```
User Action → Provider → Repository → Local DB (Isar)
                              ↓
                     Mark as "pending_sync"
                              ↓
                    Return optimistic result → Update UI
                              ↓
              (When Online) → Sync Service → Upload to API
```

---

## Technology Stack

### Backend (GoHardAPI)

| Technology | Version | Purpose |
|------------|---------|---------|
| .NET | 8.0 | Web framework |
| ASP.NET Core | 8.0 | RESTful API |
| Entity Framework Core | 8.0 | ORM for database access |
| SQL Server | 2019+ | Primary database |
| JWT Bearer | - | Authentication |
| Swagger/OpenAPI | - | API documentation |
| Azure DevOps | - | CI/CD pipeline |

### Frontend (go_hard_app)

| Technology | Version | Purpose |
|------------|---------|---------|
| Flutter | 3.7.0+ | Cross-platform UI framework |
| Dart | 3.7.0+ | Programming language |
| Provider | ^6.1.1 | State management |
| Isar | ^3.1.0 | Local NoSQL database |
| http | ^1.1.2 | HTTP client |
| flutter_secure_storage | ^9.0.0 | Secure token storage |
| connectivity_plus | ^5.0.2 | Network status monitoring |
| json_annotation | ^4.8.1 | JSON serialization |

---

## API Documentation

### Base URL
```
Development: http://localhost:5000
Production: https://your-api-url.com
```

### Authentication

All API endpoints (except login/signup) require JWT authentication.

**Headers:**
```http
Authorization: Bearer <your_jwt_token>
```

**Getting a Token:**
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password123"
}

Response:
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "userId": 1,
  "name": "John Doe",
  "email": "user@example.com"
}
```

### API Endpoints

#### Users

**Get Current User**
```http
GET /api/users/{id}
Authorization: Bearer {token}

Response: 200 OK
{
  "id": 1,
  "name": "John Doe",
  "email": "user@example.com",
  "height": 180.5,
  "weight": 75.0,
  "goals": "Build muscle"
}
```

**Update User**
```http
PUT /api/users/{id}
Content-Type: application/json
Authorization: Bearer {token}

{
  "name": "John Doe",
  "email": "user@example.com",
  "height": 180.5,
  "weight": 76.5,
  "goals": "Build muscle"
}

Response: 204 No Content
```

#### Sessions (Workouts)

**Get All Sessions**
```http
GET /api/sessions
Authorization: Bearer {token}

Response: 200 OK
[
  {
    "id": 1,
    "userId": 1,
    "date": "2024-01-15T10:00:00Z",
    "duration": 3600,
    "notes": "Great workout",
    "type": "strength",
    "status": "completed",
    "startedAt": "2024-01-15T10:00:00Z",
    "completedAt": "2024-01-15T11:00:00Z",
    "exercises": [...]
  }
]
```

**Create Session**
```http
POST /api/sessions
Content-Type: application/json
Authorization: Bearer {token}

{
  "date": "2024-01-15T10:00:00Z",
  "type": "Workout",
  "status": "draft",
  "notes": ""
}

Response: 201 Created
{
  "id": 10,
  "userId": 1,
  "date": "2024-01-15T10:00:00Z",
  ...
}
```

**Update Session Status**
```http
PATCH /api/sessions/{id}/status
Content-Type: application/json
Authorization: Bearer {token}

{
  "status": "in_progress"
}

Response: 200 OK
{
  "id": 10,
  "status": "in_progress",
  "startedAt": "2024-01-15T10:05:00Z",
  ...
}
```

**Pause Session**
```http
PATCH /api/sessions/{id}/pause
Authorization: Bearer {token}

Response: 200 OK
```

**Resume Session**
```http
PATCH /api/sessions/{id}/resume
Authorization: Bearer {token}

Response: 200 OK
```

**Delete Session**
```http
DELETE /api/sessions/{id}
Authorization: Bearer {token}

Response: 204 No Content
```

#### Exercises

**Add Exercise to Session**
```http
POST /api/sessions/{sessionId}/exercises
Content-Type: application/json
Authorization: Bearer {token}

{
  "exerciseTemplateId": 5,
  "name": "Bench Press"
}

Response: 201 Created
```

**Get Exercise Sets**
```http
GET /api/exercises/{exerciseId}/sets
Authorization: Bearer {token}

Response: 200 OK
[
  {
    "id": 1,
    "exerciseId": 10,
    "setNumber": 1,
    "reps": 10,
    "weight": 135.0,
    "isCompleted": true
  }
]
```

**Create Exercise Set**
```http
POST /api/exercises/{exerciseId}/sets
Content-Type: application/json
Authorization: Bearer {token}

{
  "setNumber": 1,
  "reps": 10,
  "weight": 135.0
}

Response: 201 Created
```

#### Exercise Templates

**Get All Templates**
```http
GET /api/exercisetemplates
Authorization: Bearer {token}

Query Parameters:
  - category (optional): Filter by category
  - muscleGroup (optional): Filter by muscle group
  - equipment (optional): Filter by equipment

Response: 200 OK
[
  {
    "id": 1,
    "name": "Bench Press",
    "category": "Strength",
    "muscleGroup": "Chest",
    "equipment": "Barbell",
    "difficulty": "Intermediate",
    "instructions": "1. Lie on bench\n2. Lower bar\n3. Press up"
  }
]
```

**Get Categories**
```http
GET /api/exercisetemplates/categories
Authorization: Bearer {token}

Response: 200 OK
["Strength", "Cardio", "Flexibility"]
```

**Get Muscle Groups**
```http
GET /api/exercisetemplates/musclegroups
Authorization: Bearer {token}

Response: 200 OK
["Chest", "Back", "Legs", "Shoulders", "Arms", "Core"]
```

### Error Responses

All endpoints return standard HTTP status codes:

```http
400 Bad Request
{
  "error": "Invalid request data",
  "details": { ... }
}

401 Unauthorized
{
  "error": "Missing or invalid authentication token"
}

404 Not Found
{
  "error": "Resource not found"
}

500 Internal Server Error
{
  "error": "An unexpected error occurred"
}
```

---

## Mobile App Documentation

### Architecture Pattern: MVVM with Provider

The app follows a clean architecture pattern inspired by MVVM:

```
UI (Screens/Widgets)
    ↓
Providers (ViewModels - State Management)
    ↓
Repositories (Business Logic + Data Source Coordination)
    ↓
Services (API, Local DB, Auth, Sync)
```

### Project Structure

```
go_hard_app/
├── lib/
│   ├── main.dart                    # App entry point
│   ├── app.dart                     # App widget with routing
│   │
│   ├── core/                        # Core utilities
│   │   ├── constants/
│   │   │   └── api_config.dart      # API endpoints
│   │   ├── services/
│   │   │   ├── connectivity_service.dart  # Network monitoring
│   │   │   └── sync_service.dart         # Auto-sync engine
│   │   └── utils/
│   │       └── database_cleanup.dart     # DB maintenance
│   │
│   ├── data/                        # Data layer
│   │   ├── models/                  # API models
│   │   │   ├── session.dart
│   │   │   ├── exercise.dart
│   │   │   ├── exercise_set.dart
│   │   │   └── exercise_template.dart
│   │   │
│   │   ├── local/                   # Local database
│   │   │   ├── models/              # Local (Isar) models
│   │   │   │   ├── local_session.dart
│   │   │   │   ├── local_exercise.dart
│   │   │   │   └── local_exercise_set.dart
│   │   │   └── services/
│   │   │       ├── local_database_service.dart  # Isar DB
│   │   │       └── model_mapper.dart            # API ↔ Local
│   │   │
│   │   ├── repositories/            # Data access layer
│   │   │   ├── session_repository.dart
│   │   │   ├── exercise_repository.dart
│   │   │   └── auth_repository.dart
│   │   │
│   │   └── services/                # External services
│   │       ├── api_service.dart     # HTTP client
│   │       └── auth_service.dart    # Auth + token storage
│   │
│   ├── providers/                   # State management
│   │   ├── auth_provider.dart
│   │   ├── sessions_provider.dart
│   │   ├── active_workout_provider.dart
│   │   ├── exercises_provider.dart
│   │   └── profile_provider.dart
│   │
│   └── ui/                          # User interface
│       ├── screens/
│       │   ├── auth/
│       │   │   ├── login_screen.dart
│       │   │   └── signup_screen.dart
│       │   ├── sessions/
│       │   │   ├── sessions_screen.dart
│       │   │   └── active_workout_screen.dart
│       │   ├── exercises/
│       │   │   └── exercises_screen.dart
│       │   └── profile/
│       │       └── profile_screen.dart
│       └── widgets/
│           └── common/
│
└── test/                            # Tests
    ├── data/
    │   ├── local/
    │   │   └── services/
    │   │       └── model_mapper_test.dart
    └── providers/
        └── auth_provider_test.dart
```

### State Management with Provider

**Dependency Injection (main.dart):**

```dart
MultiProvider(
  providers: [
    // Services (singletons)
    Provider<LocalDatabaseService>.value(value: localDb),
    ChangeNotifierProvider<ConnectivityService>.value(value: connectivity),
    Provider<AuthService>(create: (_) => AuthService()),

    // Repositories
    ProxyProvider4<ApiService, LocalDatabaseService, ConnectivityService,
                   AuthService, SessionRepository>(
      update: (_, apiService, localDb, connectivity, authService, __) =>
          SessionRepository(apiService, localDb, connectivity, authService),
    ),

    // Providers (state managers)
    ChangeNotifierProxyProvider2<SessionRepository, AuthService,
                                  SessionsProvider>(
      create: (context) => SessionsProvider(
        context.read<SessionRepository>(),
        context.read<AuthService>(),
      ),
      update: (_, sessionRepo, authService, previous) =>
          previous ?? SessionsProvider(sessionRepo, authService),
    ),
  ],
  child: const MyApp(),
)
```

**Using Providers in UI:**

```dart
class SessionsScreen extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    // Watch provider for rebuilds when state changes
    final provider = context.watch<SessionsProvider>();

    if (provider.isLoading) {
      return CircularProgressIndicator();
    }

    return ListView.builder(
      itemCount: provider.sessions.length,
      itemBuilder: (context, index) {
        final session = provider.sessions[index];
        return SessionCard(session: session);
      },
    );
  }
}
```

### Key Providers

#### AuthProvider
- Manages authentication state
- Handles login/signup
- Stores JWT token securely
- Clears local data on logout

#### SessionsProvider
- Loads and caches workout sessions
- Creates new workouts
- Deletes sessions
- Handles pull-to-refresh

#### ActiveWorkoutProvider
- Manages current workout session
- Real-time timer (pause/resume)
- Updates workout status
- Loads exercises for session

### Services

#### ApiService
```dart
class ApiService {
  final AuthService _authService;

  // GET request
  Future<T> get<T>(String endpoint) async {
    final token = await _authService.getToken();
    final response = await http.get(
      Uri.parse('${ApiConfig.baseUrl}/$endpoint'),
      headers: {
        'Authorization': 'Bearer $token',
        'Content-Type': 'application/json',
      },
    );

    if (response.statusCode == 200) {
      return json.decode(response.body) as T;
    } else {
      throw Exception('API Error: ${response.statusCode}');
    }
  }

  // POST, PUT, DELETE methods...
}
```

#### LocalDatabaseService (Isar)
```dart
class LocalDatabaseService {
  Isar? _isar;

  Future<Isar> initialize() async {
    final dir = await getApplicationDocumentsDirectory();

    _isar = await Isar.open(
      [
        LocalSessionSchema,
        LocalExerciseSchema,
        LocalExerciseSetSchema,
      ],
      directory: dir.path,
      name: 'go_hard_local_db',
      inspector: true,  // Enable Isar Inspector
    );

    return _isar!;
  }

  Isar get database => _isar!;

  Future<void> clearAll() async {
    await _isar!.writeTxn(() async {
      await _isar!.clear();
    });
  }
}
```

#### ConnectivityService
```dart
class ConnectivityService extends ChangeNotifier {
  bool _isOnline = true;

  bool get isOnline => _isOnline;
  bool get isOffline => !_isOnline;

  Stream<bool> get connectivityStream => _connectivityController.stream;

  Future<void> initialize() async {
    // Listen for connectivity changes
    _connectivitySubscription = _connectivity.onConnectivityChanged.listen((result) {
      final wasOnline = _isOnline;
      _isOnline = _hasInternetConnectivity([result]);

      if (wasOnline != _isOnline) {
        debugPrint('Connectivity changed: ${_isOnline ? "ONLINE" : "OFFLINE"}');
        _connectivityController.add(_isOnline);
        notifyListeners();
      }
    });
  }
}
```

---

## Database Schema

### SQL Server Schema (API)

#### Users Table
```sql
CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) UNIQUE NOT NULL,
    Password NVARCHAR(100) NOT NULL,  -- Hashed
    Height FLOAT NULL,
    Weight FLOAT NULL,
    Goals NVARCHAR(500) NULL,
    DateCreated DATETIME2 DEFAULT GETDATE()
);
```

#### Sessions Table
```sql
CREATE TABLE Sessions (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL,
    Date DATETIME2 NOT NULL,
    Duration INT NULL,  -- Seconds
    Notes NVARCHAR(MAX) NULL,
    Type NVARCHAR(50) NULL,
    Status NVARCHAR(50) DEFAULT 'draft',  -- draft, in_progress, completed
    StartedAt DATETIME2 NULL,
    CompletedAt DATETIME2 NULL,
    PausedAt DATETIME2 NULL,

    CONSTRAINT FK_Session_User FOREIGN KEY (UserId)
        REFERENCES Users(Id) ON DELETE CASCADE
);
```

#### Exercises Table
```sql
CREATE TABLE Exercises (
    Id INT PRIMARY KEY IDENTITY(1,1),
    SessionId INT NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Duration INT NULL,
    RestTime INT NULL,
    Notes NVARCHAR(MAX) NULL,
    ExerciseTemplateId INT NULL,

    CONSTRAINT FK_Exercise_Session FOREIGN KEY (SessionId)
        REFERENCES Sessions(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Exercise_Template FOREIGN KEY (ExerciseTemplateId)
        REFERENCES ExerciseTemplates(Id) ON DELETE SET NULL
);
```

#### ExerciseSets Table
```sql
CREATE TABLE ExerciseSets (
    Id INT PRIMARY KEY IDENTITY(1,1),
    ExerciseId INT NOT NULL,
    SetNumber INT NOT NULL,
    Reps INT NULL,
    Weight FLOAT NULL,
    Duration INT NULL,
    IsCompleted BIT DEFAULT 0,
    CompletedAt DATETIME2 NULL,
    Notes NVARCHAR(500) NULL,

    CONSTRAINT FK_ExerciseSet_Exercise FOREIGN KEY (ExerciseId)
        REFERENCES Exercises(Id) ON DELETE CASCADE
);
```

#### ExerciseTemplates Table
```sql
CREATE TABLE ExerciseTemplates (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    Category NVARCHAR(50) NULL,
    MuscleGroup NVARCHAR(50) NULL,
    Equipment NVARCHAR(100) NULL,
    Difficulty NVARCHAR(50) NULL,
    VideoUrl NVARCHAR(500) NULL,
    ImageUrl NVARCHAR(500) NULL,
    Instructions NVARCHAR(MAX) NULL,
    IsCustom BIT DEFAULT 0,
    CreatedByUserId INT NULL,

    CONSTRAINT FK_Template_User FOREIGN KEY (CreatedByUserId)
        REFERENCES Users(Id) ON DELETE SET NULL
);
```

### Relationships

```
Users (1) ──────────< (N) Sessions
Sessions (1) ────────< (N) Exercises
Exercises (1) ───────< (N) ExerciseSets
ExerciseTemplates (1) < (N) Exercises [Optional]
Users (1) ───────────< (N) ExerciseTemplates [Custom only]
```

**Cascade Delete:**
- Delete User → Deletes all their Sessions
- Delete Session → Deletes all Exercises in that session
- Delete Exercise → Deletes all Sets for that exercise
- Delete Template → Sets exercise.ExerciseTemplateId to NULL

### Isar Local Database (Mobile)

#### LocalSession
```dart
@collection
class LocalSession {
  Id localId = Isar.autoIncrement;  // Local primary key
  int? serverId;                     // Server ID (null if not synced)

  @Index()
  int userId;

  DateTime date;
  int? duration;
  String? notes;
  String? type;
  String status;
  DateTime? startedAt;
  DateTime? completedAt;
  DateTime? pausedAt;

  // Sync tracking
  @Index()
  bool isSynced;

  @Index()
  String syncStatus;  // 'synced', 'pending_create', 'pending_update', 'pending_delete'

  DateTime lastModifiedLocal;
  DateTime? lastModifiedServer;

  // Error handling
  int syncRetryCount;
  DateTime? lastSyncAttempt;
  String? syncError;
}
```

**Why Dual IDs?**
- `localId`: Isar auto-increment, always present, used for local references
- `serverId`: Server ID, null until synced with API
- This allows offline creation with local references that get mapped to server IDs later

---

## Offline Mode & Sync

### Offline-First Strategy

The app implements an **offline-first** architecture where:
1. All data operations work offline by default
2. Data is cached locally for instant access
3. Changes are queued for sync
4. Automatic sync when connectivity returns

### How It Works

#### 1. Cache-First Read

```dart
Future<List<Session>> getSessions() async {
  final db = await _localDb.database;

  if (_connectivity.isOnline) {
    try {
      // Fetch from API
      final apiSessions = await _apiService.get<List<dynamic>>('sessions');

      // Get current user ID for filtering
      final userId = await _authService.getUserId();

      // Update local cache (ONLY current user's data)
      await db.writeTxn(() async {
        for (final apiSession in apiSessions) {
          // Security: Only cache current user's sessions
          if (apiSession.userId != userId) continue;

          final localSession = ModelMapper.sessionToLocal(apiSession);
          await db.localSessions.put(localSession);
        }
      });

      return apiSessions;
    } catch (e) {
      // API failed - fallback to cache
      return await _getLocalSessions(db);
    }
  } else {
    // Offline - return cache
    return await _getLocalSessions(db);
  }
}
```

#### 2. Optimistic Write

```dart
Future<Session> createSession(Session session) async {
  if (_connectivity.isOnline) {
    try {
      // Try API first
      final apiSession = await _apiService.post('sessions', data: session.toJson());
      await _saveToLocalCache(apiSession, isSynced: true);
      return apiSession;
    } catch (e) {
      // API failed - save locally with pending status
      return await _createLocalSession(session, isPending: true);
    }
  } else {
    // Offline - save locally with pending_create status
    return await _createLocalSession(session, isPending: true);
  }
}

Future<Session> _createLocalSession(Session session, {required bool isPending}) async {
  final localSession = LocalSession(
    serverId: isPending ? null : session.id,
    userId: session.userId,
    date: session.date,
    isSynced: !isPending,
    syncStatus: isPending ? 'pending_create' : 'synced',
    lastModifiedLocal: DateTime.now(),
  );

  await db.writeTxn(() => db.localSessions.put(localSession));

  return Session(
    id: localSession.localId,  // Use local ID temporarily
    userId: localSession.userId,
    date: localSession.date,
  );
}
```

### Sync Service

The `SyncService` automatically syncs pending changes:

```dart
class SyncService {
  // Auto-sync every 5 minutes
  static const Duration _syncInterval = Duration(minutes: 5);

  Future<void> initialize() async {
    // Listen for connectivity changes
    _connectivitySubscription = _connectivity.connectivityStream.listen((isOnline) {
      if (isOnline) {
        _scheduleDebouncedSync();  // Sync when coming online
      }
    });

    // Periodic sync timer
    _periodicSyncTimer = Timer.periodic(_syncInterval, (_) {
      if (_connectivity.isOnline && !_isSyncing) {
        sync();
      }
    });
  }

  Future<void> sync() async {
    // Get current user ID for filtering
    final userId = await _authService.getUserId();
    if (userId == null) return;

    // Sync in order: Sessions → Exercises → Sets
    await _syncSessions(db, userId);
    await _syncExercises(db, userId);
    await _syncExerciseSets(db, userId);
  }

  Future<void> _syncSessions(Isar db, int userId) async {
    // Only sync current user's sessions
    final pendingSessions = await db.localSessions
        .filter()
        .isSyncedEqualTo(false)
        .userIdEqualTo(userId)
        .findAll();

    for (final session in pendingSessions) {
      switch (session.syncStatus) {
        case 'pending_create':
          await _syncCreate(session);
          break;
        case 'pending_update':
          await _syncUpdate(session);
          break;
        case 'pending_delete':
          await _syncDelete(session);
          break;
      }
    }
  }
}
```

### Data Isolation & Security

**Problem:** Multiple users on same device = all data cached locally

**Solution:** Three-layer protection:

1. **Cache-Time Filtering** - Only cache current user's data
2. **Read-Time Filtering** - Filter by userId when reading
3. **Logout Cleanup** - Wipe all local data on logout

```dart
// AuthProvider.logout()
Future<void> logout() async {
  // Clear auth token
  await _authService.clearToken();

  // Clear ALL local database data for privacy
  await _localDb.clearAll();

  // Clear state
  _isAuthenticated = false;
  _currentUserId = null;
  notifyListeners();
}
```

### Conflict Resolution: Server Wins

When syncing updates, if both local and server have changes:

```dart
Future<void> _syncUpdate(LocalSession localSession) async {
  // Fetch current server version
  final serverSession = await _apiService.get('sessions/${localSession.serverId}');
  final serverModified = DateTime.parse(serverSession['date']);

  // Check for conflict
  if (localSession.lastModifiedServer != null &&
      serverModified.isAfter(localSession.lastModifiedServer!)) {
    // SERVER WINS - discard local changes
    debugPrint('Conflict: Server wins for session ${localSession.serverId}');

    // Update local with server data
    await db.writeTxn(() async {
      localSession.status = serverSession['status'];
      localSession.notes = serverSession['notes'];
      localSession.lastModifiedServer = serverModified;
      localSession.isSynced = true;
      localSession.syncStatus = 'synced';
      await db.localSessions.put(localSession);
    });

    return;  // Don't send local changes
  }

  // No conflict - update server with local changes
  await _apiService.patch('sessions/${localSession.serverId}/status',
    data: {'status': localSession.status});

  // Mark as synced
  await db.writeTxn(() async {
    localSession.isSynced = true;
    localSession.syncStatus = 'synced';
    await db.localSessions.put(localSession);
  });
}
```

**Why Server Wins?**
- Simpler to implement and understand
- Prevents data loss (server is source of truth)
- User can always re-enter changes if needed
- Typical for fitness apps (Strong, Strava use similar approach)

---

## Development Guide

### Prerequisites

**For API (GoHardAPI):**
- .NET 8.0 SDK
- SQL Server 2019+ (LocalDB or full instance)
- Visual Studio 2022 or VS Code
- Postman (for API testing)

**For Mobile App (go_hard_app):**
- Flutter SDK 3.7.0+
- Dart 3.7.0+
- Android Studio / Xcode (for mobile)
- VS Code with Flutter extension

### Setting Up API

1. **Clone Repository**
```bash
git clone https://github.com/abdbabti01/GoHardAPI.git
cd GoHardAPI
```

2. **Update Connection String**

Edit `GoHardAPI/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=TrainingAppDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

3. **Run Migrations**
```bash
cd GoHardAPI
dotnet ef database update
```

4. **Run API**
```bash
dotnet run
```

API will be available at `http://localhost:5000`
Swagger UI at `http://localhost:5000/swagger`

### Setting Up Mobile App

1. **Navigate to App Directory**
```bash
cd go_hard_app
```

2. **Install Dependencies**
```bash
flutter pub get
```

3. **Generate Code** (for JSON serialization and Isar)
```bash
dart run build_runner build --delete-conflicting-outputs
```

4. **Update API URL**

Edit `lib/core/constants/api_config.dart`:
```dart
class ApiConfig {
  static const String baseUrl = 'http://10.0.2.2:5000/api';  // Android emulator
  // static const String baseUrl = 'http://localhost:5000/api';  // iOS simulator
  // static const String baseUrl = 'https://your-api.com/api';  // Production
}
```

5. **Run App**
```bash
flutter run
```

### Common Development Tasks

#### Adding a New API Endpoint

1. **Create/Update Model** (`GoHardAPI/Models/YourModel.cs`)
```csharp
public class YourModel
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}
```

2. **Add to DbContext** (`GoHardAPI/Data/TrainingContext.cs`)
```csharp
public DbSet<YourModel> YourModels { get; set; }
```

3. **Create Migration**
```bash
dotnet ef migrations add AddYourModel
dotnet ef database update
```

4. **Create Controller** (`GoHardAPI/Controllers/YourModelController.cs`)
```csharp
[Route("api/[controller]")]
[ApiController]
public class YourModelController : ControllerBase
{
    private readonly TrainingContext _context;

    public YourModelController(TrainingContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<YourModel>>> GetYourModels()
    {
        return await _context.YourModels.ToListAsync();
    }
}
```

#### Adding a New Screen in Flutter

1. **Create Screen File** (`lib/ui/screens/your_feature/your_screen.dart`)
```dart
class YourScreen extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    final provider = context.watch<YourProvider>();

    return Scaffold(
      appBar: AppBar(title: Text('Your Screen')),
      body: ListView.builder(
        itemCount: provider.items.length,
        itemBuilder: (context, index) {
          return ListTile(title: Text(provider.items[index].name));
        },
      ),
    );
  }
}
```

2. **Create Provider** (`lib/providers/your_provider.dart`)
```dart
class YourProvider extends ChangeNotifier {
  final YourRepository _repository;

  List<YourModel> _items = [];
  bool _isLoading = false;

  List<YourModel> get items => _items;
  bool get isLoading => _isLoading;

  YourProvider(this._repository);

  Future<void> loadItems() async {
    _isLoading = true;
    notifyListeners();

    try {
      _items = await _repository.getItems();
    } catch (e) {
      debugPrint('Error: $e');
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }
}
```

3. **Add to Dependency Injection** (`lib/main.dart`)
```dart
ChangeNotifierProxyProvider<YourRepository, YourProvider>(
  create: (context) => YourProvider(context.read<YourRepository>()),
  update: (_, repo, previous) => previous ?? YourProvider(repo),
),
```

4. **Add Route** (`lib/app.dart`)
```dart
routes: {
  '/your-screen': (context) => YourScreen(),
},
```

#### Adding Offline Support for New Entity

1. **Create Local Model** (`lib/data/local/models/local_your_model.dart`)
```dart
@collection
class LocalYourModel {
  Id localId = Isar.autoIncrement;
  int? serverId;

  String name;

  @Index()
  bool isSynced;

  @Index()
  String syncStatus;

  DateTime lastModifiedLocal;
  DateTime? lastModifiedServer;
}
```

2. **Add Mapper** (`lib/data/local/services/model_mapper.dart`)
```dart
static LocalYourModel yourModelToLocal(YourModel apiModel) {
  return LocalYourModel(
    serverId: apiModel.id,
    name: apiModel.name,
    isSynced: true,
    syncStatus: 'synced',
    lastModifiedLocal: DateTime.now(),
  );
}

static YourModel localToYourModel(LocalYourModel localModel) {
  return YourModel(
    id: localModel.serverId ?? 0,
    name: localModel.name,
  );
}
```

3. **Update Repository** with cache-first logic

4. **Update SyncService** to sync new entity type

5. **Generate Code**
```bash
dart run build_runner build --delete-conflicting-outputs
```

---

## Deployment

### API Deployment (Azure DevOps Pipeline)

The API uses Azure Pipelines for CI/CD.

**Pipeline File:** `azure-pipelines.yml`

```yaml
trigger:
  branches:
    include:
    - master
    - release/*

pool:
  name: default  # Custom agent pool

steps:
- task: NuGetCommand@2
  inputs:
    restoreSolution: '**/*.sln'

- task: VSBuild@1
  inputs:
    solution: '**/*.sln'
    msbuildArgs: '/p:DeployOnBuild=true /p:WebPublishMethod=Package'
    platform: '$(buildPlatform)'
    configuration: '$(buildConfiguration)'

- task: VSTest@2
  inputs:
    platform: '$(buildPlatform)'
    configuration: '$(buildConfiguration)'

- task: PublishBuildArtifacts@1
  inputs:
    PathtoPublish: '$(Build.ArtifactStagingDirectory)'
    ArtifactName: 'buldArtifact'
```

**Deployment Steps:**

1. Push to `master` or `release/*` branch
2. Pipeline automatically triggers
3. NuGet packages restored
4. Solution built with MSBuild
5. Tests run
6. Artifacts published
7. Deploy to IIS/Azure App Service

**Manual Deployment:**

```bash
# Publish for production
dotnet publish -c Release -o ./publish

# Copy to IIS wwwroot or deploy to Azure
```

### Mobile App Deployment

#### Android

1. **Update Version**

Edit `pubspec.yaml`:
```yaml
version: 1.0.0+1  # version+buildNumber
```

2. **Build APK**
```bash
flutter build apk --release
```

Output: `build/app/outputs/flutter-apk/app-release.apk`

3. **Build App Bundle** (for Play Store)
```bash
flutter build appbundle --release
```

Output: `build/app/outputs/bundle/release/app-release.aab`

#### iOS

1. **Build iOS**
```bash
flutter build ios --release
```

2. **Open Xcode**
```bash
open ios/Runner.xcworkspace
```

3. **Archive and Upload** via Xcode → Product → Archive

#### Web

```bash
flutter build web --release
```

Output: `build/web/`

Deploy to Firebase Hosting, Netlify, or any static hosting.

---

## Testing

### Running Tests

**API Tests:**
```bash
cd GoHardAPI
dotnet test
```

**Mobile App Tests:**
```bash
cd go_hard_app
flutter test                          # Run all tests
flutter test test/providers/          # Run specific directory
flutter test --coverage               # With coverage report
```

**Analyzer:**
```bash
flutter analyze
```

**Format Code:**
```bash
dart format .
```

### Test Structure

```
test/
├── data/
│   ├── local/
│   │   └── services/
│   │       └── model_mapper_test.dart      # Tests API ↔ Local conversion
│   └── repositories/
│       └── session_repository_test.dart    # Tests repository logic
├── providers/
│   └── auth_provider_test.dart             # Tests state management
└── ui/
    └── screens/
        └── login_screen_test.dart          # Widget tests
```

### Example Test

```dart
void main() {
  group('ModelMapper - Session Conversion', () {
    test('sessionToLocal should convert API Session to LocalSession', () {
      // Arrange
      final apiSession = Session(
        id: 123,
        userId: 456,
        date: DateTime(2024, 1, 15),
        duration: 3600,
        notes: 'Test workout',
      );

      // Act
      final localSession = ModelMapper.sessionToLocal(apiSession);

      // Assert
      expect(localSession.serverId, 123);
      expect(localSession.userId, 456);
      expect(localSession.isSynced, true);
      expect(localSession.syncStatus, 'synced');
    });
  });
}
```

### CI/CD Testing

**GitHub Actions** (recommended to add):

Create `.github/workflows/flutter.yml`:
```yaml
name: Flutter CI

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v3

    - uses: subosito/flutter-action@v2
      with:
        flutter-version: '3.7.0'

    - name: Install dependencies
      run: |
        cd go_hard_app
        flutter pub get

    - name: Run tests
      run: |
        cd go_hard_app
        flutter test

    - name: Analyze code
      run: |
        cd go_hard_app
        flutter analyze

    - name: Check formatting
      run: |
        cd go_hard_app
        dart format --set-exit-if-changed .
```

---

## Troubleshooting

### Common Issues

#### API: Database Connection Fails

**Error:** `Cannot connect to SQL Server`

**Solution:**
1. Verify SQL Server is running
2. Check connection string in `appsettings.json`
3. For LocalDB: Ensure instance name matches (e.g., `MSI\MSSQLSERVER01`)
4. Test connection:
```bash
sqlcmd -S MSI\MSSQLSERVER01 -Q "SELECT @@VERSION"
```

#### API: Migration Fails

**Error:** `Unable to create migrations`

**Solution:**
```bash
# Remove last migration
dotnet ef migrations remove

# Create fresh migration
dotnet ef migrations add InitialCreate

# Update database
dotnet ef database update
```

#### Mobile: Build Runner Issues

**Error:** `Conflicting outputs`

**Solution:**
```bash
# Clean and rebuild
flutter clean
flutter pub get
dart run build_runner build --delete-conflicting-outputs
```

#### Mobile: Isar Inspector Not Working

**Solution:**
1. Ensure `inspector: true` in Isar.open()
2. Download Isar Inspector app
3. Run on same network as emulator/device
4. Open `http://localhost:8080` in browser

#### Mobile: API Connection Refused (Android Emulator)

**Error:** `Connection refused`

**Solution:**
- Use `10.0.2.2` instead of `localhost`
- Example: `http://10.0.2.2:5000/api`

```dart
// lib/core/constants/api_config.dart
class ApiConfig {
  static const String baseUrl = 'http://10.0.2.2:5000/api';  // Android
  // static const String baseUrl = 'http://localhost:5000/api';  // iOS
}
```

#### Mobile: Sync Not Working

**Check:**
1. Network connectivity: `ConnectivityService.instance.isOnline`
2. Authentication: Token still valid
3. Pending items: `LocalDatabaseService.getPendingSyncCount()`
4. Logs: Look for sync errors

**Manual Sync:**
```dart
await context.read<SyncService>().sync();
```

#### Mobile: Data Not Persisting After Logout

**Expected Behavior:** All local data is cleared on logout for security.

**To Keep Data:** Remove `clearAll()` from `AuthProvider.logout()` (NOT recommended)

### Performance Issues

#### API Slow Queries

**Solution:** Add indexes
```csharp
// In OnModelCreating
modelBuilder.Entity<Session>()
    .HasIndex(s => new { s.UserId, s.Date });
```

#### Mobile: Slow List Scrolling

**Solution:**
1. Use `ListView.builder` (not `ListView`)
2. Add `const` constructors where possible
3. Implement pagination for large datasets

#### Mobile: Large Database

**Solution:**
```dart
// Run cleanup periodically
await LocalDatabaseService.instance.cleanupOldData(daysOld: 30);
```

---

## Architecture Decisions

### Why This Tech Stack?

#### .NET 8 + EF Core
- **Fast**: High performance, compiled language
- **Mature**: Battle-tested ORM with migrations
- **Type-safe**: Strong typing prevents errors
- **Scalable**: Handles thousands of requests/second

#### Flutter
- **Cross-platform**: One codebase → iOS, Android, Web, Desktop
- **Fast**: 60fps animations, native performance
- **Beautiful**: Material + Cupertino design built-in
- **Hot Reload**: Instant development feedback

#### Isar (not SQLite)
- **Fastest**: 10x faster than SQLite for Flutter
- **Zero SQL**: Type-safe queries, no raw SQL
- **Reactive**: Streams for real-time UI updates
- **Inspector**: Visual database debugging

#### Provider (not Riverpod/Bloc)
- **Simple**: Easy to learn and understand
- **Official**: Recommended by Flutter team
- **Flexible**: Works with any architecture
- **Testable**: Easy to mock and test

### Key Design Patterns

1. **Repository Pattern**
   - Abstracts data sources (API vs Local DB)
   - Single source of truth for data access
   - Easier to test and maintain

2. **Offline-First**
   - App works without internet
   - Better UX (instant responses)
   - Resilient to network issues

3. **Optimistic Updates**
   - UI updates immediately
   - Sync happens in background
   - Feels fast and responsive

4. **Server Wins Conflict Resolution**
   - Simpler than merge strategies
   - Prevents data corruption
   - Server is source of truth

---

## Future Enhancements

### Planned Features

- [ ] **Social Features**: Follow friends, share workouts
- [ ] **Analytics**: Charts, progress tracking, PRs
- [ ] **Photos**: Before/after photos, exercise form videos
- [ ] **Nutrition**: Meal logging, calorie tracking
- [ ] **Wearables**: Apple Watch, Fitbit integration
- [ ] **AI Coach**: Workout suggestions based on history
- [ ] **Voice Commands**: Start workout, log set by voice
- [ ] **Offline Maps**: Route tracking for running/cycling

### Technical Improvements

- [ ] **GraphQL API**: More efficient data fetching
- [ ] **Background Sync**: iOS/Android background services
- [ ] **Push Notifications**: Workout reminders, friend activity
- [ ] **Biometric Auth**: Face ID, Touch ID
- [ ] **Export Data**: CSV, PDF workout history
- [ ] **Multi-language**: i18n support
- [ ] **Dark Mode**: Theme switching
- [ ] **Unit Tests**: Increase coverage to 80%+

---

## Contributing

### Workflow

1. **Create Branch**
```bash
git checkout -b feature/your-feature-name
```

2. **Make Changes**
   - Follow existing code style
   - Add tests for new features
   - Update documentation

3. **Test**
```bash
# API
cd GoHardAPI
dotnet test

# Mobile
cd go_hard_app
flutter test
flutter analyze
dart format .
```

4. **Commit**
```bash
git add .
git commit -m "Add your feature description

Detailed explanation of changes.

🤖 Generated with Claude Code
Co-Authored-By: Your Name <your@email.com>"
```

5. **Push & PR**
```bash
git push origin feature/your-feature-name
# Create Pull Request on GitHub
```

### Code Style

**C# (.NET):**
- Follow Microsoft C# conventions
- Use PascalCase for public members
- Use camelCase for private members
- Add XML comments for public APIs

**Dart (Flutter):**
- Follow Dart style guide
- Use camelCase for variables/methods
- Use PascalCase for classes
- Run `dart format .` before commit

---

## License

This project is licensed under the MIT License.

---

## Support

**Issues:** [GitHub Issues](https://github.com/abdbabti01/GoHardAPI/issues)
**Email:** your-email@example.com
**Documentation:** This file :)

---

**Last Updated:** January 2, 2026
**Version:** 1.0.0
**Maintained by:** Development Team
