# Offline Mode Testing Guide

## Prerequisites
Before testing, **clear the app data** to start fresh:
1. Long press the app icon in the emulator
2. Tap "App info"
3. Tap "Storage"
4. Tap "Clear storage" / "Clear data"

This removes old test sessions that are causing "Not found" errors.

---

## Test Scenario 1: Basic Online Workout
**Goal:** Verify basic workout functionality when online

### Steps:
1. ✅ Turn OFF airplane mode (go online)
2. ✅ Open app and login
3. ✅ Create a new workout (tap "+ New Workout")
4. ✅ Start the workout (tap "Start" button)
   - **Expected:** Timer starts counting (00:00:01, 00:00:02...)
5. ✅ Add an exercise (tap "+", select "Bench Press")
   - **Expected:** Exercise appears with name "Bench Press"
6. ✅ Log a set (tap exercise, enter reps/weight, tap "Log Set")
   - **Expected:** Set appears in the list
7. ✅ Pause the workout (tap "Pause" button)
   - **Expected:** Timer stops, button changes to "Resume"
8. ✅ Resume the workout (tap "Resume" button)
   - **Expected:** Timer continues from where it stopped
9. ✅ Navigate away (tap back button)
10. ✅ Navigate back to the workout
   - **Expected:** Timer still running, exercises still there, sets still there

---

## Test Scenario 2: Offline Workout Creation
**Goal:** Verify full functionality while offline

### Steps:
1. ✅ Make sure you're ONLINE first and logged in
2. ✅ Wait for automatic caching (you'll see "✅ Synced X sessions", "✅ Cached X exercise templates" in logs)
3. ✅ Turn ON airplane mode (go offline)
4. ✅ Create a new workout offline
5. ✅ Start the workout
   - **Expected:** Timer starts counting
6. ✅ Add an exercise
   - **Expected:** Exercise appears with correct name (not empty)
7. ✅ Log sets
   - **Expected:** Sets appear
8. ✅ Pause/Resume
   - **Expected:** Works the same as online
9. ✅ Navigate away and back
   - **Expected:** Everything persists (timer, exercises, sets)
10. ✅ Complete the workout
11. ✅ Turn OFF airplane mode (go online)
12. ✅ Wait ~10 seconds for automatic sync
13. ✅ Check the workout on another device or server
   - **Expected:** Workout synced to server with all data

---

## Test Scenario 3: Viewing Cached Workouts Offline
**Goal:** Verify offline access to previously synced workouts

### Steps:
1. ✅ While ONLINE, create and complete a workout with exercises
2. ✅ Turn ON airplane mode (go offline)
3. ✅ Go to workouts list
   - **Expected:** All workouts visible
4. ✅ Open the workout you created
   - **Expected:** Shows all exercises, sets, duration, timer
5. ✅ Navigate away and back
   - **Expected:** Still shows all data

---

## Test Scenario 4: Offline Modifications
**Goal:** Verify modifications to existing workouts while offline

### Steps:
1. ✅ Create workout ONLINE and add 1 exercise
2. ✅ Turn ON airplane mode (go offline)
3. ✅ Open the workout
4. ✅ Add another exercise
   - **Expected:** Exercise added successfully
5. ✅ Log more sets
   - **Expected:** Sets logged
6. ✅ Navigate away and back
   - **Expected:** New exercise and sets still there
7. ✅ Turn OFF airplane mode (go online)
8. ✅ Wait for sync
   - **Expected:** Changes synced to server

---

## Common Issues and Solutions

### Issue: "Cannot update session without server ID"
- **Cause:** Old test data in database
- **Solution:** Clear app data and start fresh

### Issue: Exercises disappear when navigating away
- **Cause:** localId not being preserved (fixed in latest code)
- **Solution:** Restart app with latest code

### Issue: Timer resets to zero
- **Cause:** Session loaded from server instead of local cache (fixed)
- **Solution:** With latest code, local changes have priority

### Issue: Exercise names show as empty offline
- **Cause:** Template name not looked up (fixed)
- **Solution:** Restart app with latest code

---

## What Should Work

### ✅ Online Mode:
- Create workouts
- Start/pause/resume timer
- Add exercises (with names)
- Log sets
- Complete workouts
- Navigate away/back (data persists)
- Automatic sync

### ✅ Offline Mode:
- Create workouts
- Start/pause/resume timer
- Add exercises (with names from cached templates)
- Log sets
- Complete workouts
- Navigate away/back (data persists)
- View previously cached workouts
- All changes queued for sync

### ✅ Sync:
- Automatic when connectivity returns
- Manual trigger via pull-to-refresh
- Shows sync status indicator
- Handles conflicts (server wins)
- Retries failed operations

---

## Logs to Watch For

### Good logs:
```
✅ Local database initialized successfully
✅ ConnectivityService initialized - Status: ONLINE/OFFLINE
✅ Synced X sessions from server
✅ Cached X exercise templates
📦 Loaded session X from cache with Y exercises
⏸️ Session paused locally
▶️ Session resumed locally
➕ Created exercise "Exercise Name" locally
```

### Expected logs (not errors):
```
! Pause API failed, will sync later (normal when offline)
📴 Offline - returning cached session (normal when offline)
```

### Actual errors (should not appear after fixes):
```
Load session error: (should not happen)
Add exercise error: (should not happen)
Failed to load sessions: (should not happen)
```

---

## Testing Checklist

- [ ] App starts without errors
- [ ] Login works
- [ ] Can create workout online
- [ ] Timer starts and counts
- [ ] Can add exercises (names show)
- [ ] Can log sets
- [ ] Pause/resume works
- [ ] Navigate away/back preserves data
- [ ] Can go offline
- [ ] Can create workout offline
- [ ] Exercise names show offline
- [ ] Can add exercises offline
- [ ] Pause/resume works offline
- [ ] Navigate away/back works offline
- [ ] Sync works when back online
- [ ] No duplicate data after sync
- [ ] Can view old workouts offline
