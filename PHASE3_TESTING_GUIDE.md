# Phase 3: Testing Guide

## What's Been Implemented

### Backend (ASP.NET Core)
✅ **Models**:
- `SharedWorkout` - Community shared workouts with social features
- `SharedWorkoutLike` - Like tracking
- `SharedWorkoutSave` - Save/bookmark tracking
- `WorkoutTemplate` - Recurring workout templates with scheduling
- `WorkoutTemplateRating` - User ratings

✅ **Controllers** with full REST APIs:
- `SharedWorkoutsController` - 8 endpoints for community workouts
- `WorkoutTemplatesController` - 10 endpoints for templates

✅ **Database Schema**:
- Updated `TrainingContext` with new DbSets
- Configured relationships and indexes
- SQL setup script ready

### Frontend (Flutter)
✅ **Models**:
- `SharedWorkout` - Isar local model
- `WorkoutTemplate` - Isar local model
- `WorkoutSuggestion` - Plain Dart model

✅ **Repositories** (Offline-first):
- `SharedWorkoutRepository` - Community workouts with caching
- `WorkoutTemplateRepository` - Templates with scheduling

✅ **Providers** (State Management):
- `SharedWorkoutProvider` - Community state with optimistic updates
- `WorkoutTemplateProvider` - Template state

✅ **UI Screens**:
- `CommunityScreen` - 3 tabs (Discover, Saved, My Shares)
- `TemplatesScreen` - 2 tabs (My Templates, Community)

✅ **Infrastructure**:
- Dependency injection configured
- Routing set up
- Navigation buttons added to Sessions screen
- API endpoints configured

### Still TODO
❌ **Frontend Forms**:
- Template creation dialog
- Template editing dialog
- "Use Template" to create workout functionality
- "Use Shared Workout" to create workout functionality
- Exercise JSON parsing and display

## Testing Steps

### Step 1: Database Setup

1. Open SQL Server Management Studio (SSMS)
2. Connect to your local instance: `MSI\MSSQLSERVER01`
3. Open the file: `GoHardAPI/Scripts/Phase3_DatabaseSetup.sql`
4. Execute the script
5. Verify tables were created:
   ```sql
   SELECT name FROM sys.tables
   WHERE name IN ('SharedWorkouts', 'SharedWorkoutLikes', 'SharedWorkoutSaves',
                   'WorkoutTemplates', 'WorkoutTemplateRatings')
   ```

### Step 2: Run Backend API

1. Open terminal in `GoHardAPI` directory
2. Run: `dotnet run`
3. Navigate to `https://localhost:7190/swagger` (or whatever port it shows)
4. Verify new endpoints appear:
   - `/api/sharedworkouts` endpoints
   - `/api/workouttemplates` endpoints

### Step 3: Test Backend Endpoints (via Swagger)

**Test Workout Templates**:
1. POST `/api/workouttemplates` - Create a template
   ```json
   {
     "name": "Morning Routine",
     "description": "Quick morning workout",
     "exercisesJson": "[{\"name\":\"Push-ups\",\"sets\":3,\"reps\":10}]",
     "recurrencePattern": "daily",
     "estimatedDuration": 20,
     "category": "Strength"
   }
   ```
2. GET `/api/workouttemplates` - Should return your template
3. GET `/api/workouttemplates/community` - Should return templates

**Test Shared Workouts**:
1. POST `/api/sharedworkouts` - Share a workout
   ```json
   {
     "originalId": 1,
     "type": "session",
     "workoutName": "Leg Day",
     "description": "Heavy leg workout",
     "exercisesJson": "[{\"name\":\"Squats\",\"sets\":5,\"reps\":5}]",
     "duration": 45,
     "category": "Strength",
     "difficulty": "Advanced"
   }
   ```
2. GET `/api/sharedworkouts` - Should return the shared workout
3. POST `/api/sharedworkouts/{id}/like` - Like the workout
4. POST `/api/sharedworkouts/{id}/save` - Save the workout
5. GET `/api/sharedworkouts/saved` - Should return saved workouts

### Step 4: Run Flutter App

1. Open terminal in `go_hard_app` directory
2. Start Android emulator (or connect device)
3. Run: `flutter run`
4. Login to the app
5. Navigate to Sessions screen

### Step 5: Test Flutter UI

**Test Templates Screen**:
1. Click Templates icon (bookmark) in Sessions screen
2. Should see Templates screen with 2 tabs
3. Try refreshing
4. Data should load from backend (if connected)

**Test Community Screen**:
1. Click Community icon (people) in Sessions screen
2. Should see Community screen with 3 tabs
3. Try filtering workouts
4. Try liking/saving workouts

**Known Limitations** (TODO items):
- Template creation dialog not implemented (shows snackbar)
- Template editing not implemented (shows snackbar)
- "Use Template" button not functional
- "Use Workout" button not functional
- Exercise JSON shows as raw text (needs proper parsing)

### Step 6: Test Offline Functionality

1. While Flutter app is running, stop the backend API
2. Navigate to Templates/Community screens
3. Should still show cached data
4. Try liking/saving - should show offline error
5. Restart backend API
6. App should auto-sync when connection restored

## Expected Results

✅ Backend API compiles and runs
✅ Database tables created successfully
✅ Swagger UI shows all new endpoints
✅ Can create/read templates via API
✅ Can create/read/like/save shared workouts via API
✅ Flutter app compiles with 0 errors
✅ Can navigate to Templates and Community screens
✅ Screens load data from backend
✅ Offline mode shows cached data
✅ Social features (like/save) work with optimistic updates

## Next Steps After Testing

Once basic functionality is verified:
1. Implement template creation/editing forms
2. Implement "Use Template" to create workout from template
3. Implement "Use Shared Workout" to create workout from shared workout
4. Improve exercise JSON parsing and display
5. Add proper error handling and loading states
6. Test complete end-to-end flows
7. Commit and push all changes

## Troubleshooting

**Database Issues**:
- If script fails, check if tables already exist
- You may need to manually drop tables first
- Ensure SQL Server is running

**Backend Build Issues**:
- Run `dotnet clean` then `dotnet build`
- Check for any missing NuGet packages

**Flutter Issues**:
- Run `flutter clean` then `flutter pub get`
- Rebuild Isar schemas: `dart run build_runner build --delete-conflicting-outputs`
- Run `flutter analyze` to check for errors

**Connection Issues**:
- Check API base URL in `api_config.dart`
- Ensure backend is running before testing Flutter app
- Check firewall settings if can't connect from emulator
