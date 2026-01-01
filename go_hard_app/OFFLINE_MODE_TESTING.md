# Offline Mode - Manual Testing Guide

This document provides comprehensive manual testing scenarios for the GoHard offline mode implementation.

## Prerequisites

- Device or emulator with network connectivity controls
- Ability to toggle WiFi/cellular on and off
- Backend API server running and accessible
- User account created in the system
- Isar Inspector app (optional, for database verification)

## Test Environment Setup

1. **Install the app** on a physical device or emulator
2. **Login** with valid credentials
3. **Verify online mode** works (create a session, see it on server)
4. **Clear test data** before each test scenario (optional)

---

## Test Scenarios

### Scenario 1: Basic Offline Workout Creation

**Objective**: Verify users can create workouts while offline

**Steps**:
1. Login to the app (online)
2. Wait for initial data sync
3. **Turn OFF WiFi and cellular**
4. Verify offline banner appears at top of screen
5. Tap "New Workout" button
6. Verify workout is created
7. Add 2-3 exercises to the workout
8. Log sets for each exercise (reps, weight)
9. Complete the workout
10. Verify workout appears in sessions list
11. **Turn ON WiFi/cellular**
12. Wait 3-5 seconds for auto-sync
13. Check sync status indicator (should show "Syncing..." then "Synced")

**Expected Results**:
- ✅ Offline banner visible when disconnected
- ✅ Workout created successfully offline
- ✅ Exercises and sets logged without errors
- ✅ Data persists after app restart while offline
- ✅ Auto-sync triggers when connectivity restored
- ✅ Workout appears on server with all data
- ✅ Local data marked as synced (isSynced=true)

**Debug Logs to Verify**:
```
📴 Network disconnected - canceling sync
💾 Saved session locally: <localId>
🔄 Network connected - scheduling sync
✅ Session created with server ID: <serverId>
```

---

### Scenario 2: Server-Wins Conflict Resolution

**Objective**: Verify server data takes precedence over local changes during conflicts

**Steps**:
1. Login on **Device A** (online)
2. Create a workout session
3. Verify session syncs to server (note the session ID)
4. **Turn OFF network on Device A**
5. On Device A, modify the session status to "completed"
6. On **Device B** (or web interface), modify the same session status to "archived"
7. Add a note "Modified on Device B" to the session
8. **Turn ON network on Device A**
9. Wait for sync to complete

**Expected Results**:
- ✅ Device A's local changes are discarded
- ✅ Device A shows session status as "archived" (from server)
- ✅ Device A shows note "Modified on Device B"
- ✅ Debug logs show: "⚠️ Conflict detected - server has newer data, discarding local changes"
- ✅ No duplicate sessions created

**Debug Logs to Verify**:
```
⚠️ Conflict detected - server has newer data, discarding local changes
✅ Local session updated from server (conflict resolved)
```

---

### Scenario 3: Network Flapping (Rapid On/Off)

**Objective**: Verify debouncing prevents duplicate syncs during unstable connectivity

**Steps**:
1. Login to app (online)
2. Create a workout offline (disconnect network)
3. Rapidly toggle WiFi ON/OFF 5-6 times within 10 seconds
4. Leave WiFi ON
5. Observe sync status indicator
6. Check server for duplicate workouts

**Expected Results**:
- ✅ Only ONE sync attempt occurs (debounced by 3 seconds)
- ✅ No duplicate workouts on server
- ✅ Sync status shows "Syncing..." only once
- ✅ Final state is "Synced" with no errors

**Debug Logs to Verify**:
```
🔄 Network connected - scheduling sync
📴 Network disconnected - canceling sync
🔄 Network connected - scheduling sync
[... rapid toggling ...]
🔄 Starting sync... (only appears once after 3-second debounce)
✅ Sync completed successfully
```

---

### Scenario 4: App Kill During Sync

**Objective**: Verify sync recovery after app termination mid-sync

**Steps**:
1. Login and create 5-10 workouts offline
2. Turn ON network
3. Immediately force-kill the app (swipe away or use task manager)
4. Wait 5 seconds
5. Reopen the app
6. Observe sync status

**Expected Results**:
- ✅ App restarts without crash
- ✅ Pending workouts still marked for sync (isSynced=false)
- ✅ Auto-sync resumes within 5 seconds
- ✅ All workouts eventually sync to server
- ✅ Sync retry count incremented appropriately
- ✅ No data loss

**Debug Logs to Verify**:
```
✅ SyncService initialized
🔄 Network connected - scheduling sync
🔄 Starting sync...
  Syncing <N> sessions...
✅ Sync completed successfully
```

---

### Scenario 5: Token Expiration During Offline Period

**Objective**: Verify graceful handling of expired auth tokens

**Steps**:
1. Login to app
2. Turn OFF network
3. Create 2-3 workouts offline
4. Wait for token to expire (or manually expire on server/clear token partially)
5. Turn ON network
6. Wait for sync attempt

**Expected Results**:
- ✅ Sync catches 401 Unauthorized error
- ✅ User is prompted to re-login
- ✅ After re-login, pending data automatically syncs
- ✅ No data loss during re-authentication
- ✅ Error shown to user (not silent failure)

**Debug Logs to Verify**:
```
❌ Sync failed: <error message containing 401>
⚠️ Session <localId> sync failed (attempt 1/3): <error>
```

---

### Scenario 6: Large Dataset Sync

**Objective**: Verify performance with many pending items

**Steps**:
1. Login and turn OFF network
2. Create 20+ workout sessions offline
3. For each session, add 3-5 exercises
4. For each exercise, log 3-5 sets
5. Complete all workouts
6. Turn ON network
7. Monitor sync progress

**Expected Results**:
- ✅ Sync completes within reasonable time (<2 minutes for 20 sessions)
- ✅ Sync status shows progress (e.g., "Syncing...")
- ✅ All sessions sync successfully
- ✅ No timeout errors
- ✅ App remains responsive during sync
- ✅ Server receives all data correctly

**Performance Metrics**:
- Sync time: < 5 seconds per session
- No UI freezing
- Progress visible to user

**Debug Logs to Verify**:
```
  Syncing 20 sessions...
  Creating session <localId> on server...
  ✅ Session created with server ID: <serverId>
  [... repeated for each session ...]
✅ Sync completed successfully
```

---

### Scenario 7: Partial Sync Failure

**Objective**: Verify retry logic for failed sync items

**Steps**:
1. Create 5 workouts offline
2. Turn ON network
3. During sync, turn OFF network (interrupt mid-sync)
4. Wait 30 seconds
5. Turn ON network
6. Check sync status

**Expected Results**:
- ✅ Successfully synced items remain synced
- ✅ Failed items marked with sync error
- ✅ Retry count incremented for failed items
- ✅ Next sync attempt retries only failed items
- ✅ Eventually all items sync successfully

**Debug Logs to Verify**:
```
  Creating session <localId1> on server...
  ✅ Session created with server ID: <serverId1>
  Creating session <localId2> on server...
  ❌ Sync failed: <network error>
⚠️ Session <localId2> sync failed (attempt 1/3): <error>
[... on next sync ...]
  Creating session <localId2> on server...
  ✅ Session created with server ID: <serverId2>
```

---

### Scenario 8: Max Retries Exceeded

**Objective**: Verify error handling after max retry attempts

**Steps**:
1. Create a workout offline
2. Simulate persistent server error (e.g., 500 Internal Server Error)
3. Turn ON network
4. Wait for 3 sync attempts to fail
5. Check sync status indicator

**Expected Results**:
- ✅ Item marked as "sync_error" after 3 failed attempts
- ✅ Sync status shows "1 error"
- ✅ Tapping sync status allows manual retry
- ✅ "Retry Failed" button appears in sync dialog
- ✅ User can manually trigger retry
- ✅ Retry resets retry count and attempts sync again

**Debug Logs to Verify**:
```
⚠️ Session <localId> sync failed (attempt 1/3): <error>
⚠️ Session <localId> sync failed (attempt 2/3): <error>
❌ Session <localId> failed after 3 attempts: <error>
```

---

### Scenario 9: Offline Delete Operations

**Objective**: Verify delete operations sync correctly

**Steps**:
1. Login and create 3 workout sessions (online, let them sync)
2. Turn OFF network
3. Delete 1 synced session
4. Delete 1 local-only session (created offline, never synced)
5. Turn ON network
6. Wait for sync

**Expected Results**:
- ✅ Synced session marked as "pending_delete"
- ✅ Local-only session deleted immediately
- ✅ When online, DELETE request sent to server for synced session
- ✅ Server confirms deletion
- ✅ Local session deleted after server confirms
- ✅ No orphaned data on server or locally

**Debug Logs to Verify**:
```
📴 Offline - marking session for deletion
✅ Local-only session deleted (for local-only)
[... on sync ...]
  Deleting session <serverId> from server...
✅ Session deleted from server and locally
```

---

### Scenario 10: Pull-to-Refresh Manual Sync

**Objective**: Verify manual sync trigger via pull-to-refresh

**Steps**:
1. Create 2-3 workouts offline
2. Note they show as pending in sync status
3. Turn ON network
4. On sessions screen, pull down to refresh
5. Observe sync behavior

**Expected Results**:
- ✅ Pull-to-refresh triggers manual sync immediately
- ✅ Sync status shows "Syncing..."
- ✅ Pending workouts upload to server
- ✅ Sessions list refreshes with synced data
- ✅ Sync status shows "Synced" when complete

---

### Scenario 11: Sync Status Indicator Interaction

**Objective**: Verify sync status UI provides accurate information

**Steps**:
1. Create pending workouts offline (don't sync yet)
2. Tap sync status indicator in app bar
3. Review sync status dialog
4. Turn ON network
5. Tap "Sync Now" button in dialog
6. Observe sync progress

**Expected Results**:
- ✅ Dialog shows accurate pending count
- ✅ Dialog shows online/offline status
- ✅ "Sync Now" button appears when online with pending items
- ✅ Tapping "Sync Now" triggers immediate sync
- ✅ Dialog updates in real-time during sync
- ✅ Last sync time displayed and formatted correctly

**UI Elements to Verify**:
- Pending items: X
- Errors: 0
- Last sync: "5m ago" (or appropriate time)
- Status: Online/Offline

---

### Scenario 12: Database Cleanup

**Objective**: Verify old data cleanup works correctly

**Steps**:
1. Manually modify test data timestamps (or wait 30 days)
2. Create some old synced sessions (lastModifiedServer > 30 days ago)
3. Create recent sessions (lastModifiedServer < 30 days ago)
4. Call `LocalDatabaseService.cleanupOldData()`
5. Check database

**Expected Results**:
- ✅ Sessions older than 30 days are deleted
- ✅ Recent sessions remain untouched
- ✅ Related exercises and sets deleted with old sessions
- ✅ Unsynced sessions not deleted (even if old)
- ✅ Debug log shows count of cleaned items

**Debug Logs to Verify**:
```
🧹 Cleaned up <N> old sessions (older than 30 days)
```

---

## Edge Cases Checklist

- [ ] Create workout offline, sync, then modify offline again (should create new pending update)
- [ ] Multiple devices syncing same data simultaneously
- [ ] Background/foreground transitions during sync
- [ ] Low storage space scenarios
- [ ] Very slow network connection (not offline, but slow)
- [ ] Airplane mode toggle
- [ ] WiFi vs cellular data switching
- [ ] VPN connect/disconnect during sync

---

## Verification Tools

### Isar Inspector
1. Install Isar Inspector desktop app
2. Connect to running app
3. View local database contents in real-time
4. Verify sync status fields
5. Check for orphaned records

### Debug Logs
Enable verbose logging and filter by emoji prefixes:
- 🔄 Sync events
- ✅ Success operations
- ❌ Errors
- ⚠️ Warnings
- 📴 Offline events
- 💾 Local storage operations
- 🌐 Network changes

### Network Tools
- **Android**: Developer options → Network debugging
- **iOS**: Settings → Developer → Network Link Conditioner
- **Charles Proxy**: Monitor API requests during sync

---

## Performance Benchmarks

| Scenario | Expected Time | Max Acceptable |
|----------|---------------|----------------|
| Single session sync | < 2 seconds | 5 seconds |
| 10 sessions sync | < 20 seconds | 60 seconds |
| 50 sessions sync | < 90 seconds | 3 minutes |
| Initial app load (offline) | < 1 second | 3 seconds |
| Database cleanup (100 old sessions) | < 5 seconds | 15 seconds |

---

## Common Issues and Solutions

### Issue: Sync never completes
**Solution**: Check network connectivity, verify token validity, check server logs

### Issue: Duplicate workouts on server
**Solution**: Verify debouncing works, check for race conditions in sync logic

### Issue: Data loss after sync
**Solution**: Verify conflict resolution logic, check lastModifiedServer timestamps

### Issue: Slow sync performance
**Solution**: Check batch size, verify database indexes, profile queries

### Issue: UI freezing during sync
**Solution**: Verify all sync operations are async, check for blocking DB operations

---

## Success Criteria

All manual tests should pass with:
- ✅ No data loss in any scenario
- ✅ No duplicate data created
- ✅ Sync completes reliably
- ✅ User always knows sync status
- ✅ Errors handled gracefully
- ✅ Performance within acceptable limits
- ✅ App remains stable and responsive

---

## Reporting Issues

When reporting issues, include:
1. **Scenario**: Which test scenario failed
2. **Steps**: Exact steps to reproduce
3. **Expected**: What should have happened
4. **Actual**: What actually happened
5. **Logs**: Relevant debug logs
6. **Database State**: Isar Inspector screenshots if relevant
7. **Environment**: Device, OS version, network type

---

**Last Updated**: 2024-01-15
**Offline Mode Version**: 1.0
**Test Checklist**: Complete Phases 1-5
