# GoHardApp - Mobile Training App

## Overview

A .NET MAUI cross-platform mobile app (Android, iOS, Windows, macOS) that connects to your GoHardAPI backend.

## Features

### 1. Exercise Library
- Browse 18+ pre-loaded exercise templates
- Filter by category (Strength, Cardio, Core)
- Filter by muscle group (Chest, Back, Legs, etc.)
- View exercise details (description, equipment, difficulty, instructions)

### 2. Workout Sessions
- View all your workout sessions
- See session details (date, type, duration, exercises)
- Delete sessions (swipe left on any session)
- Pull to refresh

## Project Structure

```
GoHardApp/
├── Models/              # Data models (User, Session, Exercise, ExerciseTemplate)
├── Services/            # API communication (ApiService)
├── ViewModels/          # MVVM view models (ExercisesViewModel, SessionsViewModel)
├── Views/               # UI pages (ExercisesPage, SessionsPage)
├── Converters/          # Value converters for XAML binding
└── GlobalUsings.cs      # Global using statements
```

## How to Run

### Prerequisites
- .NET 8 SDK
- .NET MAUI workload installed (already done)
- For Android: Android SDK, Emulator or physical device
- For iOS: Mac with Xcode (required for iOS development)
- For Windows: Windows 10/11

### Running on Android Emulator

1. **Start your API backend first:**
   ```bash
   cd GoHardAPI
   dotnet run
   ```
   Note the port (e.g., http://localhost:5121)

2. **Run the mobile app:**
   ```bash
   cd GoHardApp
   dotnet build -t:Run -f net8.0-android
   ```

   Or in Visual Studio:
   - Open `GoHardAPI.sln`
   - Set `GoHardApp` as startup project
   - Select Android Emulator
   - Press F5

### Running on Windows

```bash
cd GoHardApp
dotnet build -t:Run -f net8.0-windows10.0.19041.0
```

Or in Visual Studio:
- Set target to `Windows Machine`
- Press F5

## API Configuration

The app is configured to connect to your local API:

- **Android Emulator**: Uses `http://10.0.2.2:5121/api` (10.0.2.2 is the host machine from Android emulator)
- **iOS Simulator**: Uses `http://localhost:5121/api`
- **Windows**: Uses `http://localhost:5121/api`

To change the API URL, edit `GoHardApp/Services/ApiService.cs` lines 16-22.

### For Physical Devices

If running on a physical Android/iOS device, you need to:

1. Find your computer's local IP address (e.g., `192.168.1.100`)
2. Update `ApiService.cs`:
   ```csharp
   _baseUrl = "http://192.168.1.100:5121/api";
   ```
3. Make sure both devices are on the same WiFi network
4. Update your API's CORS settings if needed

## Current Functionality

### ✅ What Works
- View all exercise templates from the API
- Filter exercises by category and muscle group
- View all workout sessions
- Delete workout sessions
- Pull-to-refresh on both pages
- Tab navigation between Exercises and Workouts

### 🚧 Not Yet Implemented
- Create new workout sessions
- Add exercises to a session
- User registration/login
- Edit sessions
- Personal records tracking
- Statistics/charts
- Offline mode

## Next Steps

Would you like me to add:

1. **Create Workout** - Add a page to log new workouts
2. **User Profile** - User registration and profile management
3. **Statistics** - Charts showing progress over time
4. **Offline Support** - Cache data locally with SQLite
5. **Push Notifications** - Reminders for workouts

## Troubleshooting

### API Connection Issues

If the app can't connect to the API:

1. Make sure your API is running (`dotnet run` in GoHardAPI folder)
2. Check the API URL in `ApiService.cs` matches your API's URL
3. For Android emulator, use `10.0.2.2` instead of `localhost`
4. Check CORS is enabled in your API (already configured)

### Build Issues

If you get build errors:

```bash
# Clean and rebuild
cd GoHardApp
rm -rf obj bin
dotnet clean
dotnet restore
dotnet build
```

## App Screenshots

The app has a clean, modern UI with:
- Tab navigation at the bottom (Workouts | Exercises)
- Pull-to-refresh on all lists
- Swipe-to-delete for sessions
- Category filter buttons
- Responsive cards showing exercise/session details

## Technology Stack

- **.NET MAUI 8.0** - Cross-platform framework
- **C# 12** - Programming language
- **MVVM Pattern** - Architecture
- **CommunityToolkit.Mvvm** - MVVM helpers
- **CommunityToolkit.Maui** - UI components
- **HttpClient** - API communication
- **System.Text.Json** - JSON serialization

Enjoy your training app! 💪
