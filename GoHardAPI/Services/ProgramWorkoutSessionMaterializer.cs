using System.Text.Json;
using GoHardAPI.Models;

namespace GoHardAPI.Services
{
    /// <summary>
    /// Builds the in-memory <see cref="Session"/> (plus its child <see cref="Exercise"/>
    /// graph) for a "create session from a program workout" request, from an already-loaded
    /// and already-ownership-checked <see cref="ProgramWorkout"/>.
    ///
    /// <para>Pure construction: it never touches the database and never calls
    /// <c>SaveChanges</c>. Both the legacy unkeyed controller path
    /// (<see cref="Controllers.SessionsController.CreateSessionFromProgramWorkout"/>) and the
    /// keyed durable-protocol path
    /// (<see cref="SessionCreateService.CreateFromProgramWorkoutAsync"/>) call this, so the
    /// materialization rules — scheduled date, initial status, and the exercise copy — cannot
    /// drift between the two.</para>
    /// </summary>
    public static class ProgramWorkoutSessionMaterializer
    {
        /// <summary>
        /// Thrown when <see cref="ProgramWorkout.ExercisesJson"/> is not a parseable JSON
        /// array of objects. The caller converts it to its own "bad source data" response
        /// (legacy path: <c>400</c> with the parse message; keyed path: <c>400</c>
        /// <c>program_workout_data_invalid</c>). No Session is persisted in either case.
        /// </summary>
        public sealed class ExercisesJsonFormatException : Exception
        {
            public ExercisesJsonFormatException(string message, Exception inner)
                : base(message, inner)
            {
            }
        }

        /// <summary>
        /// Projects <paramref name="workout"/> onto a brand-new <see cref="Session"/> for
        /// <paramref name="userId"/>, with child <see cref="Exercise"/> rows attached through
        /// the navigation collection (no ids, nothing tracked). The caller adds the Session
        /// to a context and saves.
        /// </summary>
        /// <param name="requestedProgramId">
        /// The <c>ProgramId</c> from the request body. It is stamped onto the Session
        /// verbatim (the legacy contract deliberately trusts the request over a possibly
        /// stale <see cref="ProgramWorkout.ProgramId"/>); the caller is responsible for
        /// having verified it belongs to <paramref name="userId"/>.
        /// </param>
        public static Session Build(int userId, ProgramWorkout workout, int requestedProgramId)
        {
            var program = workout.Program
                ?? throw new InvalidOperationException(
                    "ProgramWorkout.Program must be eager-loaded before Build().");

            // Use the stored ScheduledDate if available, otherwise calculate it from the
            // program start date + week/day offset. ScheduledDate is set when the program is
            // created to avoid timezone issues.
            var scheduledDate = workout.ScheduledDate?.Date
                ?? program.StartDate
                    .AddDays((workout.WeekNumber - 1) * 7 + (workout.DayNumber - 1))
                    .Date;

            // Scheduled for the future -> "planned"; scheduled for today or the past ->
            // "draft" (the user can start it immediately).
            var status = scheduledDate > DateTime.UtcNow.Date
                ? SessionStatus.Planned
                : SessionStatus.Draft;

            var session = new Session
            {
                UserId = userId,
                Date = scheduledDate,
                Name = workout.WorkoutName,
                Type = workout.WorkoutType ?? "Workout",
                Status = status,
                ProgramId = requestedProgramId,
                ProgramWorkoutId = workout.Id,
            };

            List<Dictionary<string, JsonElement>>? exercisesData;
            try
            {
                exercisesData = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(
                    workout.ExercisesJson);
            }
            catch (JsonException ex)
            {
                throw new ExercisesJsonFormatException(ex.Message, ex);
            }

            if (exercisesData != null)
            {
                foreach (var exerciseData in exercisesData)
                {
                    var exercise = new Exercise
                    {
                        Name = exerciseData.ContainsKey("name")
                            ? exerciseData["name"].GetString() ?? "Exercise"
                            : "Exercise",
                    };

                    if (exerciseData.ContainsKey("exerciseTemplateId")
                        && exerciseData["exerciseTemplateId"].ValueKind != JsonValueKind.Null)
                    {
                        exercise.ExerciseTemplateId = exerciseData["exerciseTemplateId"].GetInt32();
                    }

                    if (exerciseData.ContainsKey("notes"))
                    {
                        exercise.Notes = exerciseData["notes"].GetString();
                    }

                    if (exerciseData.ContainsKey("rest")
                        && exerciseData["rest"].ValueKind != JsonValueKind.Null)
                    {
                        exercise.RestTime = exerciseData["rest"].GetInt32();
                    }

                    // Copied verbatim, never invented here: the source ProgramWorkout is
                    // expected to already carry a persisted occurrenceKey per entry (the
                    // caller normalizes/persists it via
                    // ProgramWorkoutExerciseOccurrences.EnsurePersistedAsync before calling
                    // Build). An entry that still lacks one (never normalized, or predates
                    // this field) simply materializes with a null OccurrenceKey — no
                    // positional or name-based identity is ever guessed.
                    if (exerciseData.ContainsKey("occurrenceKey")
                        && exerciseData["occurrenceKey"].ValueKind == JsonValueKind.String)
                    {
                        exercise.OccurrenceKey = exerciseData["occurrenceKey"].GetString();
                    }

                    session.Exercises.Add(exercise);
                }
            }

            return session;
        }
    }
}
