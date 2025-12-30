using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoHardApp.Models;
using GoHardApp.Services;
using System.Collections.ObjectModel;

namespace GoHardApp.ViewModels
{
    [QueryProperty(nameof(SessionId), "sessionId")]
    public partial class ActiveWorkoutViewModel : ObservableObject
    {
        private readonly ApiService _apiService;

        [ObservableProperty]
        private int sessionId;

        [ObservableProperty]
        private Session? currentSession;

        [ObservableProperty]
        private ObservableCollection<Exercise> exercises = new();

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string workoutType = "Workout";

        [ObservableProperty]
        private TimeSpan elapsedTime;

        private System.Timers.Timer? _timer;
        private DateTime _startTime;

        public ActiveWorkoutViewModel()
        {
            _apiService = new ApiService();
        }

        partial void OnSessionIdChanged(int value)
        {
            if (value > 0)
            {
                LoadSessionCommand.Execute(null);
            }
        }

        [RelayCommand]
        public async Task LoadSessionAsync()
        {
            IsLoading = true;
            try
            {
                CurrentSession = await _apiService.GetSessionAsync(SessionId);

                if (CurrentSession != null)
                {
                    WorkoutType = CurrentSession.Type ?? "Workout";
                    Exercises.Clear();
                    foreach (var exercise in CurrentSession.Exercises)
                    {
                        Exercises.Add(exercise);
                    }

                    // Update session status to in_progress if it's still draft
                    if (CurrentSession.Status == "draft")
                    {
                        await _apiService.UpdateSessionStatusAsync(SessionId, "in_progress");
                        CurrentSession.Status = "in_progress";
                        CurrentSession.StartedAt = DateTime.UtcNow;
                    }

                    // Start timer with correct start time
                    if (CurrentSession.StartedAt.HasValue)
                    {
                        StartTimer(CurrentSession.StartedAt.Value);
                    }
                    else
                    {
                        // Fallback: use current time if StartedAt is not set
                        StartTimer(DateTime.UtcNow);
                    }
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to load workout: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task AddExerciseAsync()
        {
            await Shell.Current.GoToAsync($"addexercise?sessionId={SessionId}");
        }

        [RelayCommand]
        public async Task ViewExerciseSetsAsync(Exercise exercise)
        {
            await Shell.Current.GoToAsync($"logsets?exerciseId={exercise.Id}");
        }

        [RelayCommand]
        public async Task FinishWorkoutAsync()
        {
            var confirm = await Application.Current.MainPage.DisplayAlert(
                "Finish Workout",
                "Are you ready to complete this workout?",
                "Yes",
                "No");

            if (!confirm) return;

            try
            {
                // Update session status to completed
                await _apiService.UpdateSessionStatusAsync(SessionId, "completed");

                StopTimer();

                // Navigate back to sessions page
                await Shell.Current.GoToAsync("//main/workouts");

                await Application.Current.MainPage.DisplayAlert("Success", "Workout completed! Great job!", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to finish workout: {ex.Message}", "OK");
            }
        }

        private void StartTimer(DateTime startTime)
        {
            _startTime = startTime;

            // Stop existing timer if any
            _timer?.Stop();
            _timer?.Dispose();

            // Set initial elapsed time
            ElapsedTime = DateTime.UtcNow - _startTime;

            // Create and start new timer
            _timer = new System.Timers.Timer(1000); // Update every second
            _timer.Elapsed += (s, e) =>
            {
                ElapsedTime = DateTime.UtcNow - _startTime;
            };
            _timer.Start();
        }

        private void StopTimer()
        {
            _timer?.Stop();
            _timer?.Dispose();
        }
    }
}
