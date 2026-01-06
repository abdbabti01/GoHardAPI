# Quick Start - Continue Testing

## Background Tasks Currently Running

Check status of these tasks:

```bash
# Backend API (Task b5c066a)
cat C:\Users\babti\AppData\Local\Temp\claude\C--Users-babti-Documents-GitHub-GoHardAPI\tasks\b5c066a.output

# Android Emulator (Task b671df2)
cat C:\Users\babti\AppData\Local\Temp\claude\C--Users-babti-Documents-GitHub-GoHardAPI\tasks\b671df2.output
```

---

## Option A: Continue with Running Processes

If backend and emulator are running:

```bash
# 1. Check emulator is ready
cd go_hard_app
flutter devices

# Should show: emulator-5554 or similar

# 2. Run the app
flutter run

# 3. Wait for build (30-60 seconds)
# 4. App launches automatically
```

---

## Option B: Start Fresh

If processes stopped or you want clean start:

### Terminal 1: Backend
```bash
cd GoHardAPI
dotnet run
```

Wait for: `Now listening on: http://0.0.0.0:5121`

### Terminal 2: Emulator & App
```bash
# Launch emulator
cd go_hard_app
flutter emulators --launch Pixel_3a_API_30_x86

# Wait 20-30 seconds for boot

# Check emulator is ready
flutter devices

# Run app
flutter run
```

---

## Quick Testing Steps

Once app is running:

1. **Login:**
   - Email: `test@test.com`
   - Password: `Test123!`
   - (Or create new account)

2. **Go to Analytics Tab:**
   - Tap 4th icon in bottom navigation (bar chart icon)

3. **Test Goals:**
   - Tap **Flag icon** in top-right
   - Should see Goals screen with tabs
   - Pull to refresh to load data

4. **Test Body Metrics:**
   - Go back to Analytics
   - Tap **Weight icon** in top-right
   - Should see Body Metrics screen
   - Pull to refresh to load data

5. **Create Test Data (if empty):**
   - Open browser: `http://localhost:5121/swagger`
   - Follow: `TESTING_GUIDE.md` Part 3

---

## Verify Everything Works

### Backend Check:
```bash
curl http://localhost:5121/swagger/index.html
# Should return HTML
```

### Emulator Check:
```bash
cd go_hard_app
flutter devices
# Should show Android emulator
```

### Mobile Connection Check:
- Login should work (not timeout)
- Data loads when you pull to refresh
- If connection fails, check backend is running

---

## If Issues

### Backend Not Running:
```bash
cd GoHardAPI
dotnet run
```

### Emulator Not Found:
```bash
cd go_hard_app

# List emulators
flutter emulators

# Launch one
flutter emulators --launch Pixel_3a_API_30_x86
```

### App Won't Build:
```bash
cd go_hard_app
flutter clean
flutter pub get
flutter run
```

---

## Full Documentation

- **Complete Guide:** `TESTING_GUIDE.md`
- **Current Status:** `CURRENT_STATUS.md`
- **Project Info:** `CLAUDE.md`

---

**Everything is ready! Just run the commands above. 🚀**
