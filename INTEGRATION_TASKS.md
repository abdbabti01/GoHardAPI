# Integration Tasks - Quick Reference

## Overview
This document lists the specific tasks needed to complete the auto-tracking integrations for the GoHard fitness app.

---

## Task 1: Auto-Update Goals from Workouts ⚠️ HIGH PRIORITY

### Problem
When a user completes a workout, workout-related goals (e.g., "Workout 3 times per week") do not automatically update their progress.

### Solution Location
**File**: `GoHardAPI/Controllers/SessionsController.cs`
**Method**: `CompleteSession` (around line 140)

### Current Code
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

### Required Changes

1. **Add this new private method** to SessionsController:

```csharp
private async Task UpdateWorkoutGoals(int userId, DateTime workoutDate)
{
    // Get active workout frequency goals
    var workoutGoals = await _context.Goals
        .Where(g => g.UserId == userId &&
                    g.IsActive &&
                    !g.IsCompleted &&
                    (g.GoalType.ToLower().Contains("workout") ||
                     g.GoalType.ToLower().Contains("frequency") ||
                     g.GoalType.ToLower().Contains("training")))
        .ToListAsync();

    foreach (var goal in workoutGoals)
    {
        // Determine if this workout counts toward the goal's time frame
        bool countsTowardGoal = ShouldCountWorkout(goal, workoutDate);

        if (countsTowardGoal)
        {
            // Increment the goal's current value
            goal.CurrentValue += 1;

            // Add progress entry for tracking
            var progress = new GoalProgress
            {
                GoalId = goal.Id,
                RecordedAt = DateTime.UtcNow,
                Value = goal.CurrentValue,
                Notes = "Auto-tracked from workout completion"
            };

            _context.GoalProgressHistory.Add(progress);

            // Check if goal is now complete
            if (goal.CurrentValue >= goal.TargetValue)
            {
                goal.IsCompleted = true;
                goal.CompletedAt = DateTime.UtcNow;
                goal.IsActive = false;
            }
        }
    }
}

private bool ShouldCountWorkout(Goal goal, DateTime workoutDate)
{
    var now = DateTime.UtcNow;

    switch (goal.TimeFrame?.ToLower())
    {
        case "daily":
            return workoutDate.Date == now.Date;

        case "weekly":
            return GetWeekNumber(workoutDate) == GetWeekNumber(now) &&
                   workoutDate.Year == now.Year;

        case "monthly":
            return workoutDate.Month == now.Month &&
                   workoutDate.Year == now.Year;

        case "yearly":
            return workoutDate.Year == now.Year;

        case "total":
        case null:
            return true;  // All-time goals count any workout

        default:
            return false;
    }
}

private int GetWeekNumber(DateTime date)
{
    var culture = System.Globalization.CultureInfo.CurrentCulture;
    return culture.Calendar.GetWeekOfYear(
        date,
        System.Globalization.CalendarWeekRule.FirstDay,
        DayOfWeek.Monday
    );
}
```

2. **Modify CompleteSession method**:

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

    // AUTO-UPDATE WORKOUT GOALS
    await UpdateWorkoutGoals(userId, session.CompletedAt.Value);

    await _context.SaveChangesAsync();
    return NoContent();
}
```

### Testing Steps

1. Create a test goal:
```bash
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

2. Complete 3 workouts
3. Check goal progress:
```bash
curl -X GET http://localhost:5121/api/goals \
  -H "Authorization: Bearer <token>"
```

4. Verify `currentValue` is 3 and `isCompleted` is true

### Expected Behavior After Implementation
- ✅ User creates goal "Workout 3x per week"
- ✅ User completes first workout → goal shows 1/3
- ✅ User completes second workout → goal shows 2/3
- ✅ User completes third workout → goal shows 3/3 and marks as complete
- ✅ Goal appears as "Completed" in app

---

## Task 2: Auto-Update Goals from Body Metrics ⚠️ HIGH PRIORITY

### Problem
When a user logs body metrics (weight, body fat %), body-related goals do not automatically update.

### Solution Location
**File**: `GoHardAPI/Controllers/BodyMetricsController.cs`
**Method**: `CreateBodyMetric` (around line 50)

### Current Code
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
    await _context.SaveChangesAsync();

    return CreatedAtAction(nameof(GetBodyMetric), new { id = metric.Id }, metric);
}
```

### Required Changes

1. **Add this new private method** to BodyMetricsController:

```csharp
private async Task UpdateBodyMetricGoals(int userId, BodyMetric metric)
{
    // Get all active body-related goals
    var bodyGoals = await _context.Goals
        .Where(g => g.UserId == userId &&
                    g.IsActive &&
                    !g.IsCompleted)
        .ToListAsync();

    foreach (var goal in bodyGoals)
    {
        decimal? newValue = null;
        string goalTypeLower = goal.GoalType.ToLower();

        // Match metric type to goal type
        if (goalTypeLower.Contains("weight") && metric.Weight.HasValue)
        {
            newValue = metric.Weight.Value;
        }
        else if (goalTypeLower.Contains("bodyfat") || goalTypeLower.Contains("body fat"))
        {
            if (metric.BodyFatPercentage.HasValue)
                newValue = metric.BodyFatPercentage.Value;
        }
        else if (goalTypeLower.Contains("chest") && metric.ChestCircumference.HasValue)
        {
            newValue = metric.ChestCircumference.Value;
        }
        else if (goalTypeLower.Contains("waist") && metric.WaistCircumference.HasValue)
        {
            newValue = metric.WaistCircumference.Value;
        }
        else if (goalTypeLower.Contains("hip") && metric.HipCircumference.HasValue)
        {
            newValue = metric.HipCircumference.Value;
        }
        else if (goalTypeLower.Contains("arm") && metric.ArmCircumference.HasValue)
        {
            newValue = metric.ArmCircumference.Value;
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

            // Update goal's current value
            goal.CurrentValue = newValue.Value;

            // Check if goal is achieved
            bool goalAchieved = false;

            if (goalTypeLower.Contains("lose") || goalTypeLower.Contains("decrease"))
            {
                // For decrease goals (e.g., lose weight), check if current <= target
                goalAchieved = goal.CurrentValue <= goal.TargetValue;
            }
            else if (goalTypeLower.Contains("gain") || goalTypeLower.Contains("increase"))
            {
                // For increase goals (e.g., gain muscle), check if current >= target
                goalAchieved = goal.CurrentValue >= goal.TargetValue;
            }
            else
            {
                // For absolute goals (e.g., "reach 75kg"), check if close enough (within 0.5)
                goalAchieved = Math.Abs(goal.CurrentValue - goal.TargetValue) <= 0.5m;
            }

            if (goalAchieved)
            {
                goal.IsCompleted = true;
                goal.CompletedAt = DateTime.UtcNow;
                goal.IsActive = false;
            }
        }
    }
}
```

2. **Modify CreateBodyMetric method**:

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

### Testing Steps

1. Create a weight loss goal:
```bash
curl -X POST http://localhost:5121/api/goals \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "goalType": "Lose Weight",
    "targetValue": 75,
    "currentValue": 80,
    "unit": "kg",
    "timeFrame": "monthly",
    "startDate": "2026-01-06T00:00:00Z",
    "isActive": true,
    "isCompleted": false,
    "createdAt": "2026-01-06T00:00:00Z"
  }'
```

2. Log weight metrics:
```bash
curl -X POST http://localhost:5121/api/bodymetrics \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "weight": 78,
    "recordedAt": "2026-01-06T00:00:00Z"
  }'
```

3. Check goal progress - should show currentValue = 78

4. Log another metric at target weight:
```bash
curl -X POST http://localhost:5121/api/bodymetrics \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{
    "weight": 75,
    "recordedAt": "2026-01-13T00:00:00Z"
  }'
```

5. Check goal - should be marked as complete

### Expected Behavior After Implementation
- ✅ User creates goal "Lose 5kg" (80kg → 75kg)
- ✅ User logs weight 78kg → goal updates to show 78kg current
- ✅ User logs weight 75kg → goal marks as complete
- ✅ Progress history shows all weight logs

---

## Task 3: AI Workout Plans to Executable Workouts 🔵 MEDIUM PRIORITY

### Problem
AI generates workout plans as text messages, but users can't convert them into actual workout sessions.

### Current Behavior
```
User: "Create a chest workout"
AI: "Here's a chest workout:
1. Bench Press - 4 sets x 8-10 reps
2. Incline Dumbbell Press - 3 sets x 10-12 reps
3. Cable Flyes - 3 sets x 12-15 reps"
```

User must manually create each exercise.

### Desired Behavior
```
User: "Create a chest workout"
AI: [Same text response]
[Button appears: "Start This Workout"]
User clicks button → Workout session starts with exercises pre-populated
```

### Solution Approach

There are two approaches:

#### Approach A: Pattern Matching (Simpler)
Parse AI text responses to extract exercise information using regex.

**Frontend Changes Required**:

1. **File**: `go_hard_app/lib/ui/screens/chat/chat_screen.dart`

Add a "Parse Workout" button that appears when AI response contains workout-like text:

```dart
Widget _buildWorkoutDetector(String message) {
  // Check if message contains workout pattern
  final hasWorkout = _detectWorkoutPattern(message);

  if (!hasWorkout) return SizedBox.shrink();

  return ElevatedButton.icon(
    icon: Icon(Icons.fitness_center),
    label: Text('Start This Workout'),
    onPressed: () => _parseAndStartWorkout(message),
  );
}

bool _detectWorkoutPattern(String message) {
  // Look for patterns like "4 sets" or "x 8-10 reps"
  return message.contains(RegExp(r'\d+\s+sets?', caseSensitive: false)) ||
         message.contains(RegExp(r'x\s+\d+', caseSensitive: false));
}

Future<void> _parseAndStartWorkout(String message) async {
  // Extract exercises using regex
  final exercises = _parseExercises(message);

  if (exercises.isEmpty) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text('Could not parse workout plan')),
    );
    return;
  }

  // Create workout session with extracted exercises
  final activeWorkoutProvider = context.read<ActiveWorkoutProvider>();
  await activeWorkoutProvider.startWorkoutFromTemplate(exercises);

  // Navigate to workout screen
  Navigator.pushNamed(context, RouteNames.activeWorkout);
}

List<WorkoutExercise> _parseExercises(String message) {
  final exercises = <WorkoutExercise>[];
  final lines = message.split('\n');

  for (final line in lines) {
    // Match patterns like:
    // "1. Bench Press - 4 sets x 8-10 reps"
    // "Squats: 4x10"
    final match = RegExp(
      r'(\d+\.\s*)?([A-Za-z\s]+?)\s*[-:]\s*(\d+)\s*(?:sets?\s*)?x?\s*(\d+)',
      caseSensitive: false,
    ).firstMatch(line);

    if (match != null) {
      final name = match.group(2)?.trim() ?? '';
      final sets = int.tryParse(match.group(3) ?? '') ?? 3;
      final reps = int.tryParse(match.group(4) ?? '') ?? 10;

      exercises.add(WorkoutExercise(
        name: name,
        sets: sets,
        targetReps: reps,
      ));
    }
  }

  return exercises;
}
```

2. **File**: `go_hard_app/lib/providers/active_workout_provider.dart`

Add method to start workout from template:

```dart
Future<void> startWorkoutFromTemplate(List<WorkoutExercise> exercises) async {
  // Create new session
  final session = Session(
    id: 0,
    userId: _authService.currentUserId,
    date: DateTime.now(),
    duration: 0,
    status: 'active',
    startedAt: DateTime.now(),
    name: 'AI Generated Workout',
    type: 'strength',
  );

  // Start session
  await startWorkout(session);

  // Add exercises
  for (final exercise in exercises) {
    await addExercise(exercise);
  }

  notifyListeners();
}
```

#### Approach B: Structured AI Responses (Better, More Complex)

Modify AI to return structured JSON alongside text response.

**Backend Changes Required**:

1. **File**: `GoHardAPI/Controllers/ChatController.cs`

Update system prompt to request structured output:

```csharp
var systemMessage = @"You are a fitness AI assistant. When creating workout plans,
include structured data in your response using this format:

[WORKOUT_PLAN_START]
{
  ""name"": ""Workout Name"",
  ""exercises"": [
    {
      ""name"": ""Exercise Name"",
      ""sets"": 4,
      ""reps"": ""8-10"",
      ""rest"": 60,
      ""notes"": ""Optional notes""
    }
  ]
}
[WORKOUT_PLAN_END]

Include this JSON block at the end of your text response when providing workout plans.";
```

2. **Frontend**: Parse JSON block and use it

This approach is more reliable but requires more work on both backend and frontend.

### Recommendation
Start with **Approach A** (pattern matching) as it's simpler and doesn't require backend changes. If parsing accuracy is poor, upgrade to **Approach B**.

### Testing Steps

1. Ask AI: "Create a chest workout"
2. Verify "Start This Workout" button appears
3. Click button
4. Verify workout starts with exercises pre-loaded
5. Complete workout and verify it saves correctly

---

## Task 4: Goal Creation from Workout Patterns 🟢 LOW PRIORITY

### Idea
Suggest goals to users based on their workout history.

### Examples
- User completes 3 workouts/week for 2 weeks → Suggest "Maintain 3x/week routine"
- User increases bench press from 60kg to 70kg → Suggest "Reach 80kg bench press"
- User logs weight going down → Suggest "Continue weight loss to Xkg"

### Implementation Location
**New Endpoint**: `GoalsController.cs`

```csharp
[HttpGet("suggestions")]
public async Task<ActionResult<List<GoalSuggestion>>> GetGoalSuggestions()
{
    var userId = GetCurrentUserId();
    if (userId == 0) return Unauthorized();

    var suggestions = new List<GoalSuggestion>();

    // Analyze workout frequency
    var recentWorkouts = await _context.Sessions
        .Where(s => s.UserId == userId &&
                    s.Status == "completed" &&
                    s.Date >= DateTime.UtcNow.AddDays(-30))
        .CountAsync();

    var avgPerWeek = recentWorkouts / 4.0;

    if (avgPerWeek >= 2)
    {
        suggestions.Add(new GoalSuggestion
        {
            Title = $"Maintain {(int)Math.Round(avgPerWeek)}x/week routine",
            GoalType = "WorkoutFrequency",
            TargetValue = (int)Math.Round(avgPerWeek),
            Unit = "workouts",
            TimeFrame = "weekly"
        });
    }

    // Add more suggestions based on metrics, PRs, etc.

    return Ok(suggestions);
}
```

---

## Priority Order

1. **🔴 CRITICAL**: Task 1 - Auto-update goals from workouts
2. **🔴 CRITICAL**: Task 2 - Auto-update goals from body metrics
3. **🟡 MEDIUM**: Task 3 - AI workout plans to executable workouts
4. **🟢 NICE TO HAVE**: Task 4 - Goal suggestions

---

## Testing Checklist

After implementing all tasks:

### Workout Goals
- [ ] Create "Workout 3x/week" goal
- [ ] Complete workout → goal shows 1/3
- [ ] Complete 2nd workout → goal shows 2/3
- [ ] Complete 3rd workout → goal completes
- [ ] Goal appears in Analytics as complete

### Body Metric Goals
- [ ] Create "Lose 5kg" goal (80kg → 75kg)
- [ ] Log weight 78kg → goal shows current: 78kg
- [ ] Log weight 75kg → goal completes
- [ ] Progress history shows all logs

### AI Workouts
- [ ] Ask AI for workout plan
- [ ] "Start This Workout" button appears
- [ ] Click button → workout starts
- [ ] Exercises pre-populated correctly
- [ ] Can complete workout normally

### Integration
- [ ] Completing AI workout updates workout frequency goal
- [ ] Goals display in Analytics Overview
- [ ] Progress bars show correct percentage
- [ ] Completed goals move to completed section

---

*Priority: Implement Task 1 and Task 2 first - these provide the most value to users*
*Estimated Time: 2-3 hours for both tasks*
