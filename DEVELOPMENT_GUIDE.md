# GoHard Fitness App - Development Guide

## Table of Contents
1. [Architecture Overview](#architecture-overview)
2. [Current Implementation Status](#current-implementation-status)
3. [Backend API Structure](#backend-api-structure)
4. [Frontend Architecture](#frontend-architecture)
5. [Data Flow & Integration](#data-flow--integration)
6. [Missing Integrations](#missing-integrations)
7. [How to Continue Development](#how-to-continue-development)

---

## Architecture Overview

### Tech Stack
- **Backend**: ASP.NET Core 8.0, Entity Framework Core, SQL Server
- **Frontend**: Flutter (Android), Dart
- **Authentication**: JWT tokens
- **State Management**: Provider pattern
- **Local Storage**: Isar database (offline support)
- **API Communication**: Dio HTTP client

### Project Structure
```
GoHardAPI/
├── GoHardAPI/                 # Backend API
│   ├── Controllers/           # API endpoints
│   ├── Models/                # Data models
│   ├── Data/                  # DbContext & migrations
│   └── Program.cs             # API configuration
│
└── go_hard_app/               # Flutter mobile app
    ├── lib/
    │   ├── core/              # Constants, utils, services
    │   ├── data/              # Models, repositories, services
    │   ├── providers/         # State management
    │   └── ui/                # Screens and widgets
    └── test/                  # Unit tests
```

---

## Current Implementation Status

### ✅ Fully Implemented Features

#### 1. **User Authentication & Profile**
- **Backend**: `AuthController.cs`, `UsersController.cs`
- **Frontend**: `auth_provider.dart`, `profile_provider.dart`
- **Features**:
  - Registration with email/password
  - Login with JWT token
  - Profile management (name, bio, height, DOB, experience level, goals)
  - Profile photo upload
  - Unit preferences (Metric/Imperial)
  - Theme preferences

#### 2. **Workout Tracking**
- **Backend**: `SessionsController.cs`
- **Frontend**: `sessions_provider.dart`, `active_workout_provider.dart`
- **Features**:
  - Create and start workout sessions
  - Add exercises with sets, reps, weight
  - Rest timer between sets
  - Pause/resume/complete workouts
  - Offline support with Isar database
  - Automatic sync when online

#### 3. **Exercise Templates**
- **Backend**: `ExerciseTemplatesController.cs`
- **Frontend**: Exercise selection in workout screen
- **Features**:
  - Pre-seeded exercise library (18 exercises)
  - Filter by category, muscle group, equipment
  - Custom exercise creation
  - Exercise instructions and images

#### 4. **Analytics & Statistics**
- **Backend**: Computed from Sessions/Exercises data
- **Frontend**: `analytics_provider.dart`, `analytics_screen.dart`
- **Features**:
  - Total workouts, current streak, longest streak
  - Calendar heatmap (workout consistency)
  - Volume over time charts
  - Exercise progress tracking
  - Personal records (PRs)
  - Muscle group distribution chart

#### 5. **Goals Tracking (Manual)**
- **Backend**: `GoalsController.cs`, `Goal.cs`, `GoalProgress.cs`
- **Frontend**: `goals_provider.dart`, `goals_screen.dart`
- **API Endpoints**:
  - `GET /api/goals` - Get all user goals
  - `POST /api/goals` - Create goal
  - `PUT /api/goals/{id}` - Update goal
  - `DELETE /api/goals/{id}` - Delete goal
  - `POST /api/goals/{id}/progress` - Add progress entry
  - `PUT /api/goals/{id}/complete` - Mark complete
- **Features**:
  - Create goals with target values, time frames
  - Manual progress tracking
  - Auto-complete when target reached
  - Progress history
  - Display in Analytics Overview tab

#### 6. **Body Metrics Tracking**
- **Backend**: `BodyMetricsController.cs`, `BodyMetric.cs`
- **Frontend**: `body_metrics_provider.dart`, `body_metrics_screen.dart`
- **API Endpoints**:
  - `GET /api/bodymetrics` - Get all metrics
  - `POST /api/bodymetrics` - Log new metric
  - `PUT /api/bodymetrics/{id}` - Update metric
  - `DELETE /api/bodymetrics/{id}` - Delete metric
- **Measurements**: Weight, body fat %, chest, waist, hip, arm, thigh, calf
- **Features**:
  - Log body measurements with photos
  - Track over time
  - Unit conversion (kg/lbs, cm/inches)

#### 7. **AI Chat & Workout Plans**
- **Backend**: `ChatController.cs` (Claude API integration)
- **Frontend**: `chat_provider.dart`, `chat_screen.dart`
- **Features**:
  - Ask fitness questions
  - Get personalized workout plans
  - Context-aware responses (knows user profile)
  - Conversation history

#### 8. **Social Features**
- **Backend**: `SharedWorkoutsController.cs`
- **Frontend**: Community workouts screen
- **Features**:
  - Share completed workouts
  - View community workouts
  - Like workouts
  - Copy workouts to your library

---

## Backend API Structure

### Database Schema

#### User Table
```csharp
public class User
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
    public string PasswordHash { get; set; }
    public string PasswordSalt { get; set; }
    public DateTime DateCreated { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public double? Height { get; set; }  // Stored in CM (metric)
    public double? Weight { get; set; }  // Stored in KG (metric)
    public string? UnitPreference { get; set; }  // "Metric" or "Imperial"
    public string? ExperienceLevel { get; set; }
    public string? PrimaryGoal { get; set; }

    // Navigation properties
    public ICollection<Session> Sessions { get; set; }
    public ICollection<Goal> Goals { get; set; }
    public ICollection<BodyMetric> BodyMetrics { get; set; }
}
```

#### Session Table (Workouts)
```csharp
public class Session
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime Date { get; set; }
    public int Duration { get; set; }  // In seconds
    public string Status { get; set; }  // "active", "paused", "completed"
    public string? Type { get; set; }
    public DateTime? StartedAt { get; set; }

    // Navigation properties
    public User User { get; set; }
    public ICollection<Exercise> Exercises { get; set; }
}
```

#### Goal Table
```csharp
public class Goal
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string GoalType { get; set; }  // "Weight", "WorkoutFrequency", "Volume", etc.
    public decimal TargetValue { get; set; }
    public decimal CurrentValue { get; set; }
    public string? Unit { get; set; }  // "kg", "workouts", "%", etc.
    public string? TimeFrame { get; set; }  // "daily", "weekly", "monthly", "yearly"
    public DateTime StartDate { get; set; }
    public DateTime? TargetDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public User User { get; set; }
    public ICollection<GoalProgress> ProgressHistory { get; set; }

    // Calculated property
    public double ProgressPercentage => TargetValue > 0 ? (double)(CurrentValue / TargetValue * 100) : 0;
}
```

#### GoalProgress Table
```csharp
public class GoalProgress
{
    public int Id { get; set; }
    public int GoalId { get; set; }
    public DateTime RecordedAt { get; set; }
    public decimal Value { get; set; }
    public string? Notes { get; set; }

    // Navigation property
    public Goal Goal { get; set; }
}
```

#### BodyMetric Table
```csharp
public class BodyMetric
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime RecordedAt { get; set; }
    public decimal? Weight { get; set; }  // Stored in KG (metric)
    public decimal? BodyFatPercentage { get; set; }
    public decimal? ChestCircumference { get; set; }  // Stored in CM (metric)
    public decimal? WaistCircumference { get; set; }
    public decimal? HipCircumference { get; set; }
    public decimal? ArmCircumference { get; set; }
    public decimal? ThighCircumference { get; set; }
    public decimal? CalfCircumference { get; set; }
    public string? Notes { get; set; }
    public string? PhotoUrl { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation property
    public User User { get; set; }
}
```

### Key Backend Controllers

#### GoalsController.cs
Location: `GoHardAPI/Controllers/GoalsController.cs`

**Important Notes**:
- `CreateGoal` (line 79): Backend overrides `CurrentValue = 0` on creation
- `AddProgress` (line 190): Automatically updates goal's `CurrentValue` and marks as complete if target reached
- All datetime fields use `DateTime.UtcNow`
- User ID is extracted from JWT token, not passed in request body

#### BodyMetricsController.cs
Location: `GoHardAPI/Controllers/BodyMetricsController.cs`

**Important Notes**:
- All measurements stored in metric (kg, cm) on backend
- Frontend handles unit conversion for display
- Photos stored as URLs (implementation pending)

---

## Frontend Architecture

### State Management (Provider Pattern)

#### Key Providers

1. **AuthProvider** (`lib/providers/auth_provider.dart`)
   - Manages authentication state
   - JWT token storage/retrieval
   - Login/logout/register

2. **SessionsProvider** (`lib/providers/sessions_provider.dart`)
   - Manages workout sessions list
   - Offline/online sync
   - Session CRUD operations

3. **ActiveWorkoutProvider** (`lib/providers/active_workout_provider.dart`)
   - Manages currently active workout
   - Exercise tracking
   - Set/rep recording
   - Rest timer

4. **GoalsProvider** (`lib/providers/goals_provider.dart`)
   - Manages goals list
   - Goal CRUD operations
   - Progress tracking
   - **Does NOT auto-update from workouts/metrics**

5. **BodyMetricsProvider** (`lib/providers/body_metrics_provider.dart`)
   - Manages body metrics list
   - Metric CRUD operations
   - **Does NOT auto-update goals**

6. **AnalyticsProvider** (`lib/providers/analytics_provider.dart`)
   - Calculates workout statistics
   - Generates charts data
   - Exercise progress tracking

### Data Models

#### Goal Model (`lib/data/models/goal.dart`)
```dart
@JsonSerializable(includeIfNull: false)  // Important: excludes null fields from JSON
class Goal {
  final int id;
  final int userId;
  final String goalType;
  final double targetValue;
  final double currentValue;
  final String? unit;
  final String? timeFrame;
  final DateTime startDate;
  final DateTime? targetDate;
  final bool isActive;
  final bool isCompleted;
  final DateTime? completedAt;
  final DateTime createdAt;
  final List<GoalProgress>? progressHistory;

  // Calculated property
  double get progressPercentage => targetValue > 0 ? (currentValue / targetValue * 100) : 0;
}
```

**Important**: After changing this model, run:
```bash
cd go_hard_app
flutter pub run build_runner build --delete-conflicting-outputs
```

### Screens Structure

#### Analytics Screen (`lib/ui/screens/analytics/analytics_screen.dart`)

**Tabs**:
1. **Overview Tab**:
   - 4 key stat cards (Total Workouts, Current Streak, This Week, PRs)
   - Streak counter widget
   - Calendar heatmap widget
   - **Active Goals section** (NEW - shows top 3 active goals with progress bars)
   - Volume over time chart

2. **Progress Tab**:
   - Muscle group distribution chart
   - Exercise progress list (clickable for details)

3. **Records Tab**:
   - Personal records list

#### Goals Screen (`lib/ui/screens/goals/goals_screen.dart`)

**Features**:
- Lists active and completed goals separately
- Create goal dialog (lines 297-510)
- Add progress dialog (lines 512-643)
- Delete confirmation
- Progress percentage display
- Click goal to view progress history

**Forms**:
- `CreateGoalDialog`: Goal type, target value, current value, unit, time frame, target date
- `AddProgressDialog`: New progress value, optional notes

#### Body Metrics Screen (`lib/ui/screens/body_metrics/body_metrics_screen.dart`)

**Features**:
- Lists recent body metrics
- Add metric dialog (lines 348-655)
- Charts (line charts for weight and body fat trends)
- Delete confirmation

---

## Data Flow & Integration

### Current Integration Points

#### 1. Workouts → Analytics ✅
```
User completes workout
    ↓
SessionsProvider.completeSession()
    ↓
POST /api/sessions/{id}/complete
    ↓
AnalyticsProvider.loadAnalytics()
    ↓
Analytics screen updates (streak, charts, stats)
```

#### 2. Goals → Analytics (Display Only) ✅
```
User opens Analytics
    ↓
AnalyticsScreen.initState()
    ↓
GoalsProvider.loadGoals()
    ↓
Active goals displayed in Overview tab
    ↓
Shows progress bars (manually updated data)
```

### Missing Integration Points ❌

#### 1. Workouts → Goals (Auto-Update)
**Problem**: Completing workouts doesn't update workout-related goals

**Example Scenario**:
- User has goal: "Workout 3 times per week"
- User completes a workout
- Goal's `currentValue` should auto-increment
- **Currently**: User must manually add progress

**Solution Needed**:
- When workout completed, check for workout-frequency goals
- Auto-create GoalProgress entry
- Update goal's currentValue

**Where to Implement**:
- Backend: `SessionsController.CompleteSession()` method
- Should query user's active goals
- For goals with type "WorkoutFrequency", increment progress

#### 2. Body Metrics → Goals (Auto-Update)
**Problem**: Logging body metrics doesn't update body-related goals

**Example Scenario**:
- User has goal: "Lose 5kg" (start: 80kg, target: 75kg)
- User logs weight: 78kg
- Goal's `currentValue` should update to show 2kg lost
- **Currently**: Goal and body metrics are separate

**Solution Needed**:
- When body metric logged, check for related goals
- Update goals with matching metric type (Weight, BodyFat, etc.)
- Calculate progress automatically

**Where to Implement**:
- Backend: `BodyMetricsController.CreateBodyMetric()` method
- Query user's active goals
- For weight goals, calculate difference and update
- For body fat goals, use absolute values

#### 3. AI Plans → Executable Workouts
**Problem**: AI generates workout plans as text, not actionable workouts

**Example Scenario**:
- User asks AI: "Create a chest workout"
- AI responds with exercise list (text)
- User can't "Start Workout" from this plan
- **Currently**: User must manually recreate workout

**Solution Needed**:
- Extend AI prompt to return structured JSON
- Parse AI response into workout template
- Add "Use This Plan" button to create session
- Pre-populate exercises in ActiveWorkout

**Where to Implement**:
- Backend: Modify `ChatController` to support structured responses
- Frontend: Parse AI message for workout data
- Create `WorkoutTemplate` from AI plan
- Start session with template

---

## Missing Integrations - Detailed Implementation Guide

### 1. Auto-Update Goals from Workouts

#### Backend Changes Required

**File**: `GoHardAPI/Controllers/SessionsController.cs`

**Method to Modify**: `CompleteSession` (around line 140)

**Current Code**:
```csharp
[HttpPut("{id}/complete")]
public async Task<IActionResult> CompleteSession(int id)
{
    var userId = GetCurrentUserId();
    if (userId == 0) return Unauthorized();

    var session = await _context.Sessions.FindAsync(id);
    if (session == null || session.UserId != userId) return NotFound();

    session.Status = "completed";
    session.CompletedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync();
    return NoContent();
}
```

**New Code Needed**:
```csharp
[HttpPut("{id}/complete")]
public async Task<IActionResult> CompleteSession(int id)
{
    var userId = GetCurrentUserId();
    if (userId == 0) return Unauthorized();

    var session = await _context.Sessions.FindAsync(id);
    if (session == null || session.UserId != userId) return NotFound();

    session.Status = "completed";
    session.CompletedAt = DateTime.UtcNow;

    // AUTO-UPDATE WORKOUT FREQUENCY GOALS
    await UpdateWorkoutGoals(userId, session.CompletedAt.Value);

    await _context.SaveChangesAsync();
    return NoContent();
}

private async Task UpdateWorkoutGoals(int userId, DateTime workoutDate)
{
    // Get active workout frequency goals
    var workoutGoals = await _context.Goals
        .Where(g => g.UserId == userId &&
                    g.IsActive &&
                    g.GoalType.Contains("Workout") || g.GoalType.Contains("Frequency"))
        .ToListAsync();

    foreach (var goal in workoutGoals)
    {
        // Determine if workout counts toward goal time frame
        bool countsTowardGoal = false;

        if (goal.TimeFrame == "daily" && workoutDate.Date == DateTime.UtcNow.Date)
            countsTowardGoal = true;
        else if (goal.TimeFrame == "weekly" && GetWeekNumber(workoutDate) == GetWeekNumber(DateTime.UtcNow))
            countsTowardGoal = true;
        else if (goal.TimeFrame == "monthly" && workoutDate.Month == DateTime.UtcNow.Month)
            countsTowardGoal = true;

        if (countsTowardGoal)
        {
            // Add progress entry
            var progress = new GoalProgress
            {
                GoalId = goal.Id,
                RecordedAt = DateTime.UtcNow,
                Value = goal.CurrentValue + 1,
                Notes = "Auto-tracked from workout completion"
            };

            _context.GoalProgressHistory.Add(progress);

            // Update goal current value
            goal.CurrentValue += 1;

            // Check if goal is complete
            if (goal.CurrentValue >= goal.TargetValue && !goal.IsCompleted)
            {
                goal.IsCompleted = true;
                goal.CompletedAt = DateTime.UtcNow;
                goal.IsActive = false;
            }
        }
    }
}

private int GetWeekNumber(DateTime date)
{
    var culture = System.Globalization.CultureInfo.CurrentCulture;
    return culture.Calendar.GetWeekOfYear(date,
        System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday);
}
```

### 2. Auto-Update Goals from Body Metrics

#### Backend Changes Required

**File**: `GoHardAPI/Controllers/BodyMetricsController.cs`

**Method to Modify**: `CreateBodyMetric` (around line 50)

**Add Method**:
```csharp
private async Task UpdateBodyMetricGoals(int userId, BodyMetric metric)
{
    // Get active weight and body composition goals
    var bodyGoals = await _context.Goals
        .Where(g => g.UserId == userId &&
                    g.IsActive &&
                    (g.GoalType.Contains("Weight") ||
                     g.GoalType.Contains("BodyFat") ||
                     g.GoalType.Contains("Body")))
        .ToListAsync();

    foreach (var goal in bodyGoals)
    {
        decimal? newValue = null;

        // Match goal type to metric
        if (goal.GoalType.Contains("Weight") && metric.Weight.HasValue)
        {
            // For weight loss goals, calculate how much lost
            // Assuming goal.CurrentValue stores kg lost from start
            // and TargetValue is total kg to lose
            newValue = metric.Weight.Value;
        }
        else if (goal.GoalType.Contains("BodyFat") && metric.BodyFatPercentage.HasValue)
        {
            newValue = metric.BodyFatPercentage.Value;
        }

        if (newValue.HasValue)
        {
            // Add progress entry
            var progress = new GoalProgress
            {
                GoalId = goal.Id,
                RecordedAt = DateTime.UtcNow,
                Value = newValue.Value,
                Notes = "Auto-tracked from body metric log"
            };

            _context.GoalProgressHistory.Add(progress);

            // Update goal current value
            goal.CurrentValue = newValue.Value;

            // For weight loss: check if target reached
            // This logic depends on how you structure weight goals
            // Example: if goal is "reach 75kg" and current is 75kg
            if (goal.CurrentValue <= goal.TargetValue && !goal.IsCompleted)
            {
                goal.IsCompleted = true;
                goal.CompletedAt = DateTime.UtcNow;
                goal.IsActive = false;
            }
        }
    }
}
```

**Call in CreateBodyMetric**:
```csharp
[HttpPost]
public async Task<ActionResult<BodyMetric>> CreateBodyMetric(BodyMetric metric)
{
    var userId = GetCurrentUserId();
    if (userId == 0) return Unauthorized();

    metric.UserId = userId;
    metric.RecordedAt = DateTime.UtcNow;
    metric.CreatedAt = DateTime.UtcNow;

    _context.BodyMetrics.Add(metric);

    // AUTO-UPDATE BODY-RELATED GOALS
    await UpdateBodyMetricGoals(userId, metric);

    await _context.SaveChangesAsync();

    return CreatedAtAction(nameof(GetBodyMetric), new { id = metric.Id }, metric);
}
```

### 3. AI Plans to Executable Workouts

This requires a more complex implementation with structured AI responses.

#### Option A: Structured Prompts (Simpler)

**Modify Chat Prompt** to request JSON format:
```csharp
var systemMessage = @"You are a fitness AI. When creating workout plans,
respond with JSON in this format:

{
  ""message"": ""Here's your chest workout..."",
  ""workout"": {
    ""name"": ""Chest Workout"",
    ""exercises"": [
      {
        ""name"": ""Bench Press"",
        ""sets"": 4,
        ""reps"": ""8-10"",
        ""notes"": ""Focus on form""
      }
    ]
  }
}";
```

**Frontend**: Parse JSON and create workout from it

#### Option B: New Endpoint (Better)

**Create**: `WorkoutTemplatesController.cs`
```csharp
[HttpPost("ai-generate")]
public async Task<ActionResult<WorkoutTemplate>> GenerateAIWorkout(
    [FromBody] AIWorkoutRequest request)
{
    // Call Claude API with structured prompt
    // Parse response
    // Create WorkoutTemplate
    // Return to client
}
```

---

## How to Continue Development

### Step 1: Set Up Environment

#### Backend Setup
```bash
cd GoHardAPI
dotnet restore
dotnet ef database update  # Apply migrations
dotnet run  # Start API on http://localhost:5121
```

#### Frontend Setup
```bash
cd go_hard_app
flutter pub get
flutter run -d <device_id>  # Use emulator-5554 for Android
```

### Step 2: Test Current Features

#### Create Test User
```bash
curl -X POST http://localhost:5121/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test123!",
    "name": "Test User"
  }'
```

#### Test Goals API
```bash
# Login first to get token
curl -X POST http://localhost:5121/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test123!"}'

# Use token in subsequent requests
curl -X POST http://localhost:5121/api/goals \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "goalType": "WorkoutFrequency",
    "targetValue": 3,
    "currentValue": 0,
    "unit": "workouts",
    "timeFrame": "weekly",
    "startDate": "2026-01-06T00:00:00Z",
    "isActive": true,
    "isCompleted": false,
    "createdAt": "2026-01-06T00:00:00Z"
  }'
```

### Step 3: Implement Auto-Tracking

1. **Implement Workout → Goals Auto-Update**
   - Add `UpdateWorkoutGoals()` method to SessionsController
   - Test by completing a workout and checking goal progress

2. **Implement Body Metrics → Goals Auto-Update**
   - Add `UpdateBodyMetricGoals()` method to BodyMetricsController
   - Test by logging weight and checking weight goals

3. **Test Integration**
   - Create a "Workout 3x per week" goal
   - Complete 3 workouts
   - Verify goal auto-updates and completes

### Step 4: Improve AI Integration

1. **Update Chat Prompt** to return structured responses
2. **Create Workout Template Parser** in frontend
3. **Add "Use This Plan" Button** in chat screen
4. **Test** by asking AI for a workout plan and starting it

### Key Files to Modify

#### For Auto-Tracking Goals:
1. `GoHardAPI/Controllers/SessionsController.cs` (line ~140)
2. `GoHardAPI/Controllers/BodyMetricsController.cs` (line ~50)
3. No frontend changes needed (automatic backend behavior)

#### For AI Workout Plans:
1. `GoHardAPI/Controllers/ChatController.cs` (modify prompt)
2. `go_hard_app/lib/ui/screens/chat/chat_screen.dart` (add parse & use plan)
3. `go_hard_app/lib/providers/active_workout_provider.dart` (load from template)

---

## Important Notes

### JSON Serialization
- Goal model uses `@JsonSerializable(includeIfNull: false)`
- After model changes, run: `flutter pub run build_runner build --delete-conflicting-outputs`
- Null fields are excluded from JSON (important for progressHistory)

### DateTime Handling
- Backend stores all times in UTC
- Frontend converts to local time for display
- Always use `DateTime.UtcNow` in backend
- Always use `.toUtc()` when sending to backend from frontend

### Unit Conversions
- Backend stores: kg for weight, cm for height/circumference
- Frontend displays based on user's UnitPreference
- Conversion happens in `UnitConverter` class (`lib/core/utils/unit_converter.dart`)

### Authentication
- JWT tokens stored in Flutter secure storage
- Token passed in `Authorization: Bearer <token>` header
- API automatically injects token via Dio interceptor
- Backend extracts user ID from token, not request body

### Offline Support
- Workouts use Isar for offline storage
- Goals and Body Metrics require online connection
- Sessions sync automatically when connection restored

---

## Testing Checklist

### Before Considering "Production Ready":

- [ ] Workouts auto-update workout frequency goals
- [ ] Body metrics auto-update weight/body composition goals
- [ ] AI workout plans can be converted to executable workouts
- [ ] All forms validate correctly
- [ ] Unit conversions work properly (kg/lbs, cm/inches)
- [ ] Offline workout tracking works
- [ ] Analytics correctly shows integrated data
- [ ] Profile changes reflect everywhere
- [ ] Goals display in Analytics Overview
- [ ] Charts and statistics calculate correctly

### User Flow Testing:

1. [ ] New user can register and complete profile
2. [ ] User can create a workout frequency goal
3. [ ] User completes a workout → goal auto-updates
4. [ ] User creates a weight loss goal
5. [ ] User logs weight → goal auto-updates
6. [ ] User asks AI for workout plan → can start workout from it
7. [ ] User completes workout → appears in analytics
8. [ ] User views analytics → sees goals progress

---

## Contact & Resources

### Useful Commands
```bash
# Rebuild JSON serialization
cd go_hard_app && flutter pub run build_runner build --delete-conflicting-outputs

# Clean and rebuild Flutter
flutter clean && flutter pub get && flutter run

# View backend logs
# (Check task output in terminal)

# Database migrations
dotnet ef migrations add <Name> --project GoHardAPI
dotnet ef database update --project GoHardAPI
```

### API Base URL
- Development: `http://10.0.2.2:5121/api` (Android emulator)
- Configured in: `go_hard_app/lib/core/constants/api_config.dart`

### Database Connection
- Server: `MSI\MSSQLSERVER01` (local SQL Server)
- Database: `TrainingAppDb`
- Connection string in: `GoHardAPI/appsettings.json`

---

## Next Steps Priority

1. **HIGH**: Implement workout → goals auto-tracking
2. **HIGH**: Implement body metrics → goals auto-tracking
3. **MEDIUM**: Add AI workout plan execution
4. **MEDIUM**: Add goal suggestions based on user stats
5. **LOW**: Add more body metric charts
6. **LOW**: Add workout plan templates library

---

*Last Updated: 2026-01-06*
*Version: 1.0*
*Status: Core features complete, auto-tracking pending*
