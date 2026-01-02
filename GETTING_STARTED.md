# Getting Started - Complete Setup Guide

> **Step-by-step instructions for new developers to set up, run, and test the GoHard Fitness App**

This guide assumes you have NO prior setup. We'll walk through everything from scratch.

---

## Table of Contents

1. [Prerequisites Installation](#prerequisites-installation)
2. [Project Setup](#project-setup)
3. [Running the API](#running-the-api)
4. [Running the Mobile App](#running-the-mobile-app)
5. [Testing Everything](#testing-everything)
6. [Troubleshooting](#troubleshooting)

---

## Prerequisites Installation

### Step 1: Install Git

**Windows:**
1. Download Git from https://git-scm.com/download/win
2. Run the installer
3. Accept all defaults
4. Verify installation:
```bash
git --version
# Should output: git version 2.x.x
```

**macOS:**
```bash
# Install Homebrew first (if not installed)
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"

# Install Git
brew install git

# Verify
git --version
```

**Linux (Ubuntu/Debian):**
```bash
sudo apt update
sudo apt install git
git --version
```

---

### Step 2: Install .NET 8 SDK (for API)

**All Platforms:**

1. Go to https://dotnet.microsoft.com/download/dotnet/8.0
2. Download ".NET 8.0 SDK" (NOT just Runtime)
3. Run the installer
4. Restart your terminal
5. Verify installation:
```bash
dotnet --version
# Should output: 8.0.x
```

**Verify .NET Components:**
```bash
dotnet --list-sdks
# Should show: 8.0.xxx [path]

dotnet --list-runtimes
# Should show ASP.NET Core 8.0.x
```

---

### Step 3: Install SQL Server

**Windows:**

**Option A: SQL Server Express (Recommended for development)**

1. Download from https://www.microsoft.com/en-us/sql-server/sql-server-downloads
2. Click "Download now" under "Express"
3. Run installer
4. Select "Basic" installation
5. Accept defaults
6. Note the **instance name** (e.g., `SQLEXPRESS` or `MSI\MSSQLSERVER01`)
7. Install SQL Server Management Studio (SSMS) when prompted

**Option B: SQL Server LocalDB (Lightweight)**

1. Download from https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb
2. Run installer
3. Verify:
```bash
sqllocaldb info
# Should list MSSQLLocalDB
```

**macOS/Linux:**

Use Docker:
```bash
# Pull SQL Server Docker image
docker pull mcr.microsoft.com/mssql/server:2022-latest

# Run SQL Server container
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong@Password" \
  -p 1433:1433 --name sql-server \
  -d mcr.microsoft.com/mssql/server:2022-latest

# Verify
docker ps
# Should show sql-server container running
```

**Verify SQL Server Connection:**

**Using SQLCMD:**
```bash
# Windows (Express)
sqlcmd -S localhost\SQLEXPRESS -U sa -P YourPassword

# Windows (LocalDB)
sqlcmd -S (localdb)\MSSQLLocalDB

# macOS/Linux (Docker)
sqlcmd -S localhost,1433 -U sa -P YourStrong@Password

# Inside SQLCMD
SELECT @@VERSION;
GO

# Exit
QUIT
```

---

### Step 4: Install Flutter SDK

**Windows:**

1. Download Flutter SDK from https://docs.flutter.dev/get-started/install/windows
2. Extract to `C:\src\flutter` (or any location WITHOUT spaces)
3. Add to PATH:
   - Open Start → Search "Environment Variables"
   - Click "Environment Variables"
   - Under "User variables", select "Path" → "Edit"
   - Click "New" → Add `C:\src\flutter\bin`
   - Click OK
4. Restart terminal
5. Verify:
```bash
flutter --version
# Should output Flutter 3.x.x
```

**macOS:**

```bash
# Download Flutter
cd ~
git clone https://github.com/flutter/flutter.git -b stable --depth 1

# Add to PATH (add to ~/.zshrc or ~/.bash_profile)
echo 'export PATH="$PATH:$HOME/flutter/bin"' >> ~/.zshrc

# Reload shell
source ~/.zshrc

# Verify
flutter --version
```

**Linux:**

```bash
# Download Flutter
cd ~
git clone https://github.com/flutter/flutter.git -b stable --depth 1

# Add to PATH
echo 'export PATH="$PATH:$HOME/flutter/bin"' >> ~/.bashrc
source ~/.bashrc

# Install dependencies
sudo apt install clang cmake ninja-build pkg-config libgtk-3-dev

# Verify
flutter --version
```

**Run Flutter Doctor:**
```bash
flutter doctor
```

This will check for missing dependencies. Follow the instructions to fix any issues.

**Common Flutter Doctor Fixes:**

**Android toolchain:**
1. Install Android Studio from https://developer.android.com/studio
2. Open Android Studio → More Actions → SDK Manager
3. Install:
   - Android SDK Platform (API 34)
   - Android SDK Build-Tools
   - Android SDK Command-line Tools
4. Run `flutter doctor --android-licenses` and accept all

**Visual Studio (Windows only for desktop apps):**
1. Install Visual Studio 2022 Community
2. Select "Desktop development with C++"

**Xcode (macOS only for iOS):**
1. Install from App Store
2. Run: `sudo xcode-select --switch /Applications/Xcode.app/Contents/Developer`
3. Run: `sudo xcodebuild -runFirstLaunch`

---

### Step 5: Install IDE/Editor

**Option A: Visual Studio Code (Recommended)**

1. Download from https://code.visualstudio.com
2. Install
3. Install extensions:
   - **C# Dev Kit** (for .NET)
   - **Flutter** (includes Dart)
   - **SQLTools** (for database queries)

**Option B: Visual Studio 2022 (Windows - for .NET)**

1. Download from https://visualstudio.microsoft.com
2. Install workloads:
   - ASP.NET and web development
   - .NET desktop development

**Option C: Android Studio (For mobile development)**

1. Download from https://developer.android.com/studio
2. Install Flutter plugin

---

## Project Setup

### Step 1: Clone Repository

```bash
# Navigate to where you want the project
cd ~/Documents  # or C:\Users\YourName\Documents on Windows

# Clone repository
git clone https://github.com/abdbabti01/GoHardAPI.git

# Navigate into project
cd GoHardAPI
```

**Verify structure:**
```bash
ls -la  # or dir on Windows

# You should see:
# - GoHardAPI/ (API project)
# - go_hard_app/ (Flutter app)
# - README.md
# - azure-pipelines.yml
```

---

### Step 2: Set Up API Database

**1. Update Connection String**

Open `GoHardAPI/appsettings.json` in your editor:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "YOUR_CONNECTION_STRING_HERE"
  },
  "Jwt": {
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLong",
    "Issuer": "GoHardAPI",
    "Audience": "GoHardApp"
  }
}
```

**Connection Strings for Different Setups:**

**Windows - SQL Server Express:**
```json
"Server=localhost\\SQLEXPRESS;Database=TrainingAppDb;Trusted_Connection=True;TrustServerCertificate=True"
```

**Windows - Named Instance (e.g., MSI\MSSQLSERVER01):**
```json
"Server=MSI\\MSSQLSERVER01;Database=TrainingAppDb;Trusted_Connection=True;TrustServerCertificate=True"
```

**Windows - LocalDB:**
```json
"Server=(localdb)\\MSSQLLocalDB;Database=TrainingAppDb;Trusted_Connection=True;TrustServerCertificate=True"
```

**Docker / SQL Server with password:**
```json
"Server=localhost,1433;Database=TrainingAppDb;User Id=sa;Password=YourStrong@Password;TrustServerCertificate=True"
```

**2. Install EF Core Tools**

```bash
dotnet tool install --global dotnet-ef

# Verify
dotnet ef --version
# Should output: Entity Framework Core .NET Command-line Tools 8.0.x
```

**3. Create Database**

```bash
cd GoHardAPI

# Create database and run migrations
dotnet ef database update

# You should see:
# Build started...
# Build succeeded.
# Applying migration '20240115_InitialCreate'.
# Done.
```

**4. Verify Database Created**

**Using SQLCMD:**
```bash
# Connect to SQL Server (adjust for your setup)
sqlcmd -S localhost\SQLEXPRESS -E

# List databases
SELECT name FROM sys.databases;
GO

# You should see TrainingAppDb listed

# Switch to TrainingAppDb
USE TrainingAppDb;
GO

# List tables
SELECT name FROM sys.tables;
GO

# You should see: Users, Sessions, Exercises, ExerciseSets, ExerciseTemplates
```

**Using SQL Server Management Studio (SSMS):**
1. Open SSMS
2. Connect to `localhost\SQLEXPRESS` (or your instance)
3. Expand "Databases"
4. You should see "TrainingAppDb"
5. Expand TrainingAppDb → Tables
6. You should see 5 tables + __EFMigrationsHistory

---

### Step 3: Set Up Mobile App

**1. Navigate to Flutter Project**

```bash
cd go_hard_app
```

**2. Install Dependencies**

```bash
flutter pub get

# You should see:
# Resolving dependencies...
# Got dependencies!
```

**3. Generate Code**

The project uses code generation for JSON serialization and Isar database:

```bash
dart run build_runner build --delete-conflicting-outputs

# This will take 20-30 seconds
# You should see:
# [INFO] Generating build script...
# [INFO] Running build...
# [INFO] Succeeded after XXs with XX outputs
```

**4. Update API URL**

Open `lib/core/constants/api_config.dart`:

```dart
class ApiConfig {
  // CHOOSE ONE based on your setup:

  // Android Emulator → localhost API
  static const String baseUrl = 'http://10.0.2.2:5000/api';

  // iOS Simulator → localhost API
  // static const String baseUrl = 'http://localhost:5000/api';

  // Physical device on same WiFi
  // static const String baseUrl = 'http://192.168.1.XXX:5000/api';  // Your computer's local IP

  // Production API
  // static const String baseUrl = 'https://your-api.com/api';

  // Auth endpoints
  static const String login = 'auth/login';
  static const String signup = 'auth/signup';
  static const String sessions = 'sessions';
  static const String exerciseTemplates = 'exercisetemplates';
}
```

**Finding Your Local IP (for physical device):**

**Windows:**
```bash
ipconfig
# Look for "IPv4 Address" under your WiFi adapter
# Example: 192.168.1.105
```

**macOS/Linux:**
```bash
ifconfig
# Look for "inet" under en0 or wlan0
# Example: 192.168.1.105
```

Then use:
```dart
static const String baseUrl = 'http://192.168.1.105:5000/api';
```

---

## Running the API

### Step 1: Navigate to API Project

```bash
cd GoHardAPI  # If not already there
```

### Step 2: Restore Packages

```bash
dotnet restore

# You should see:
# Determining projects to restore...
# Restored GoHardAPI.csproj (in XXms).
```

### Step 3: Build Project

```bash
dotnet build

# You should see:
# Build succeeded.
#     0 Warning(s)
#     0 Error(s)
```

**If you get build errors:**
- Check .NET version: `dotnet --version` (should be 8.0.x)
- Clean and rebuild:
```bash
dotnet clean
dotnet build
```

### Step 4: Run API

```bash
dotnet run

# You should see:
# Building...
# info: Microsoft.Hosting.Lifetime[14]
#       Now listening on: http://localhost:5000
# info: Microsoft.Hosting.Lifetime[0]
#       Application started. Press Ctrl+C to shut down.
```

**The API is now running!**

**Test API in Browser:**

Open your browser and go to:
```
http://localhost:5000/swagger
```

You should see the **Swagger UI** with all API endpoints documented.

**Test Login Endpoint:**

1. In Swagger, find **POST /api/auth/signup**
2. Click "Try it out"
3. Enter:
```json
{
  "name": "Test User",
  "email": "test@example.com",
  "password": "password123"
}
```
4. Click "Execute"
5. You should get **200 OK** with a JWT token

**Keep this terminal open** - the API needs to stay running.

---

## Running the Mobile App

### Step 1: Open New Terminal

Keep the API terminal running and open a **new terminal** for Flutter.

### Step 2: Navigate to Flutter Project

```bash
cd path/to/GoHardAPI/go_hard_app
```

### Step 3: Connect Device/Emulator

**Option A: Android Emulator**

1. Open Android Studio
2. Click "Device Manager" (phone icon on right side)
3. Click "Create Device"
4. Select "Pixel 7" → Next
5. Select "API 34" system image → Next
6. Name it "Pixel_7_API_34" → Finish
7. Click ▶️ to start emulator

**Verify device connected:**
```bash
flutter devices

# You should see:
# Pixel 7 API 34 (mobile) • emulator-5554 • android • Android 14 (API 34)
```

**Option B: iOS Simulator (macOS only)**

```bash
# List available simulators
xcrun simctl list devices

# Open simulator
open -a Simulator

# Or use flutter
flutter emulators
flutter emulators --launch apple_ios_simulator
```

**Option C: Physical Device**

**Android:**
1. Enable Developer Mode on phone:
   - Settings → About Phone → Tap "Build Number" 7 times
2. Enable USB Debugging:
   - Settings → Developer Options → USB Debugging → On
3. Connect phone via USB
4. On phone, tap "Allow" when prompted

**iOS:**
1. Connect iPhone via USB
2. Trust computer on iPhone
3. In Xcode: Window → Devices and Simulators
4. Click "Trust" on iPhone

**Verify:**
```bash
flutter devices

# Should show your device
```

### Step 4: Run Flutter App

```bash
flutter run

# Or specify device:
flutter run -d emulator-5554        # Android emulator
flutter run -d "iPhone 15 Pro Max"  # iOS simulator
flutter run -d <device-id>          # Physical device
```

**First run takes 3-5 minutes** to build. You'll see:

```
Launching lib/main.dart on Pixel 7 API 34 in debug mode...
Running Gradle task 'assembleDebug'...
✓ Built build/app/outputs/flutter-apk/app-debug.apk.
Installing app...
```

**App should launch on your device!**

### Step 5: Create Account & Test

1. **Signup Screen** appears first
2. Enter:
   - Name: Test User
   - Email: test@example.com
   - Password: password123
3. Tap "SIGN UP"
4. You should be logged in and see the **Sessions Screen**

**Test Workflow:**

1. **Create Workout:**
   - Tap ➕ button
   - You should see "Active Workout" screen

2. **Add Exercise:**
   - Tap "+ ADD EXERCISE"
   - Select "Bench Press" from library
   - Tap exercise card

3. **Log Sets:**
   - Enter Reps: 10
   - Enter Weight: 135
   - Tap "ADD SET"
   - Repeat for more sets

4. **Start Timer:**
   - Tap "START" button
   - Timer should start counting

5. **Pause/Resume:**
   - Tap "PAUSE" (timer pauses)
   - Tap "RESUME" (timer continues)
   - **Verify:** No flicker when pausing/resuming

6. **Complete Workout:**
   - Tap "FINISH WORKOUT"
   - Confirm
   - Should return to Sessions list
   - Your workout appears at top

7. **Test Offline Mode:**
   - Turn off WiFi on device
   - Create new workout
   - Add exercises
   - Log sets
   - Turn WiFi back on
   - Wait 5-10 seconds
   - Check API logs - should see sync requests

---

## Testing Everything

### Test API Independently

**Using Postman or cURL:**

**1. Signup:**
```bash
curl -X POST http://localhost:5000/api/auth/signup \
  -H "Content-Type: application/json" \
  -d '{
    "name": "API Test",
    "email": "apitest@example.com",
    "password": "password123"
  }'

# Response: { "token": "eyJhbGc...", "userId": 2, ... }
```

**2. Login:**
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "apitest@example.com",
    "password": "password123"
  }'

# Copy the token from response
```

**3. Get Sessions (requires token):**
```bash
curl -X GET http://localhost:5000/api/sessions \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"

# Response: [ ... array of sessions ... ]
```

### Run Automated Tests

**API Tests:**
```bash
cd GoHardAPI
dotnet test

# You should see:
# Passed! - Failed: 0, Passed: X, Skipped: 0, Total: X
```

**Flutter Tests:**
```bash
cd go_hard_app
flutter test

# You should see:
# 00:02 +29: All tests passed!
```

**Flutter Analyzer:**
```bash
flutter analyze

# You should see:
# No issues found!
```

**Format Check:**
```bash
dart format .

# Should format all files
# Formatted 77 files
```

### Test Offline Mode

**Scenario 1: Create Workout Offline**

1. Turn off WiFi on device
2. In app, create new workout
3. Add 2 exercises
4. Log 3 sets each
5. Complete workout
6. Turn WiFi back on
7. **Check API logs** - should see POST requests syncing data
8. **Check Swagger** - GET /api/sessions should show the workout

**Scenario 2: Network Flapping**

1. Start workout online
2. Quickly toggle WiFi on/off 5 times
3. Add exercises while toggling
4. Keep WiFi on for 30 seconds
5. **Verify:** No duplicate data in Swagger
6. **Verify:** All exercises appear correctly

**Scenario 3: Data Isolation**

1. Create user "alice@example.com"
2. Create 3 workouts as Alice
3. Logout
4. Create user "bob@example.com"
5. Create 2 workouts as Bob
6. **Turn off WiFi**
7. Pull to refresh
8. **Verify:** Only Bob's 2 workouts appear (NOT Alice's 3)
9. Logout
10. **Verify:** All local data cleared (check with Isar Inspector)

### Inspect Local Database

**Isar Inspector:**

1. Run app in debug mode
2. Download Isar Inspector: https://isar.dev/inspector
3. Open Isar Inspector app
4. Connect to `localhost:8080` (should auto-detect)
5. Browse collections:
   - localSessions
   - localExercises
   - localExerciseSets
   - localExerciseTemplates
6. Check sync status, pending items, etc.

---

## Troubleshooting

### API Issues

#### Error: "Database connection failed"

**Check:**
1. SQL Server running?
```bash
# Windows
net start MSSQL$SQLEXPRESS

# Docker
docker ps  # Should show sql-server container
```

2. Connection string correct in `appsettings.json`?

3. Test connection:
```bash
sqlcmd -S localhost\SQLEXPRESS -E
SELECT @@VERSION;
GO
```

#### Error: "Table doesn't exist"

**Run migrations:**
```bash
cd GoHardAPI
dotnet ef database update
```

**If that fails, reset database:**
```bash
dotnet ef database drop
dotnet ef database update
```

#### Error: "Port 5000 already in use"

**Find and kill process:**

**Windows:**
```bash
netstat -ano | findstr :5000
taskkill /PID <PID> /F
```

**macOS/Linux:**
```bash
lsof -i :5000
kill -9 <PID>
```

**Or change port in `Program.cs`:**
```csharp
builder.WebHost.UseUrls("http://localhost:5001");
```

### Flutter Issues

#### Error: "flutter: command not found"

**Fix PATH:**

**Windows:**
1. Search "Environment Variables"
2. Edit System Path
3. Add `C:\src\flutter\bin`
4. Restart terminal

**macOS/Linux:**
```bash
echo 'export PATH="$PATH:$HOME/flutter/bin"' >> ~/.zshrc
source ~/.zshrc
```

#### Error: "No devices found"

**Android Emulator:**
```bash
# List emulators
flutter emulators

# Create one if none exist
# Open Android Studio → Device Manager → Create Device

# Launch emulator
flutter emulators --launch <emulator-id>
```

**Physical Device:**
- USB Debugging enabled?
- Trust computer prompt accepted?
- Try different USB cable/port

#### Error: "Build failed" or "Gradle errors"

**Clean and rebuild:**
```bash
cd go_hard_app
flutter clean
flutter pub get
dart run build_runner build --delete-conflicting-outputs
flutter run
```

#### Error: "Connection refused" from app

**Android Emulator:**
- Use `http://10.0.2.2:5000/api` (NOT `localhost`)

**iOS Simulator:**
- Use `http://localhost:5000/api`

**Physical Device:**
- Use your computer's local IP: `http://192.168.1.XXX:5000/api`
- Make sure phone and computer on same WiFi

**Verify API reachable:**
```bash
# From device browser, open:
http://10.0.2.2:5000/swagger  # Android emulator
http://192.168.1.XXX:5000/swagger  # Physical device
```

#### Error: "Isar database not found"

**Initialize database:**
```bash
cd go_hard_app
flutter clean
flutter run
```

Database auto-initializes on first run.

**Check initialization in logs:**
```
I/flutter ( 1234): ✅ Local database initialized successfully
I/flutter ( 1234): 📊 Database path: /data/user/0/com.example.go_hard_app/app_flutter/go_hard_local_db
```

### Common Build Errors

#### "Cannot find package xxx"

```bash
# API
cd GoHardAPI
dotnet restore
dotnet build

# Flutter
cd go_hard_app
flutter pub get
```

#### "Code generation failed"

```bash
cd go_hard_app
flutter clean
dart run build_runner clean
dart run build_runner build --delete-conflicting-outputs
```

#### "Version conflict"

**Check versions:**
```bash
dotnet --version  # Should be 8.0.x
flutter --version  # Should be 3.7.0+
```

**Update Flutter:**
```bash
flutter upgrade
flutter doctor
```

---

## Next Steps

Now that everything is running:

1. **Explore the Code:**
   - Start with `README.md` for architecture overview
   - Check `lib/providers/` for state management
   - Look at `lib/data/repositories/` for data access

2. **Make a Change:**
   - Add a new field to User model
   - Create a new screen
   - Add a new API endpoint

3. **Read Documentation:**
   - Main README: `README.md`
   - This guide: `GETTING_STARTED.md`
   - API endpoints: http://localhost:5000/swagger

4. **Join Development:**
   - Create a branch: `git checkout -b feature/your-feature`
   - Make changes
   - Run tests: `flutter test` and `dotnet test`
   - Commit and push

---

## Quick Reference Commands

```bash
# API
cd GoHardAPI
dotnet restore              # Install packages
dotnet build                # Build project
dotnet run                  # Run API (port 5000)
dotnet test                 # Run tests
dotnet ef database update   # Update database

# Flutter
cd go_hard_app
flutter pub get             # Install packages
flutter run                 # Run app
flutter test                # Run tests
flutter analyze             # Check code quality
dart format .               # Format code
dart run build_runner build --delete-conflicting-outputs  # Generate code

# Git
git status                  # Check status
git add .                   # Stage all changes
git commit -m "message"     # Commit
git push                    # Push to remote
git pull                    # Pull updates

# Useful
flutter devices             # List connected devices
flutter doctor              # Check Flutter setup
dotnet --version            # Check .NET version
git log --oneline           # View commit history
```

---

## Summary Checklist

Setup Complete When:

- [ ] Git installed and `git --version` works
- [ ] .NET 8 SDK installed and `dotnet --version` shows 8.0.x
- [ ] SQL Server installed and `sqlcmd` connects
- [ ] Flutter installed and `flutter doctor` shows no critical issues
- [ ] IDE/Editor installed with extensions
- [ ] Repository cloned: `cd GoHardAPI` works
- [ ] API database created: TrainingAppDb exists
- [ ] API runs: http://localhost:5000/swagger opens
- [ ] Mobile app dependencies installed: `flutter pub get` succeeded
- [ ] Code generated: `dart run build_runner build` succeeded
- [ ] Device/emulator connected: `flutter devices` shows device
- [ ] App runs: Signup/login screen appears
- [ ] Tests pass: `flutter test` and `dotnet test` succeed
- [ ] Can create workout, add exercises, log sets
- [ ] Offline mode works: Create workout without WiFi

**If all checked ✅ - You're ready to develop!**

---

**Questions? Issues?**

- Check [Troubleshooting](#troubleshooting) section
- Read main [README.md](README.md)
- Check GitHub Issues
- Ask the team on Slack/Discord

**Happy Coding! 🚀**
