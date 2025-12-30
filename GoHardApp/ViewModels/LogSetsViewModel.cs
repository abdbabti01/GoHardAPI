using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoHardApp.Models;
using GoHardApp.Services;
using System.Collections.ObjectModel;

namespace GoHardApp.ViewModels
{
    [QueryProperty(nameof(ExerciseId), "exerciseId")]
    public partial class LogSetsViewModel : ObservableObject
    {
        private readonly ApiService _apiService;

        [ObservableProperty]
        private int exerciseId;

        [ObservableProperty]
        private Exercise? currentExercise;

        [ObservableProperty]
        private ObservableCollection<ExerciseSet> sets = new();

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private int newSetReps;

        [ObservableProperty]
        private double newSetWeight;

        public LogSetsViewModel()
        {
            _apiService = new ApiService();
        }

        partial void OnExerciseIdChanged(int value)
        {
            if (value > 0)
            {
                LoadExerciseCommand.Execute(null);
            }
        }

        [RelayCommand]
        public async Task LoadExerciseAsync()
        {
            IsLoading = true;
            try
            {
                // Note: We would need to add a GetExerciseAsync method to ApiService
                // For now, we'll load sets directly
                var exerciseSets = await _apiService.GetExerciseSetsByExerciseAsync(ExerciseId);
                Sets.Clear();
                foreach (var set in exerciseSets.OrderBy(s => s.SetNumber))
                {
                    Sets.Add(set);
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to load sets: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task AddSetAsync()
        {
            if (NewSetReps <= 0)
            {
                await Application.Current.MainPage.DisplayAlert("Validation", "Please enter reps", "OK");
                return;
            }

            try
            {
                var newSet = new ExerciseSet
                {
                    ExerciseId = ExerciseId,
                    SetNumber = Sets.Count + 1,
                    Reps = NewSetReps,
                    Weight = NewSetWeight > 0 ? NewSetWeight : null,
                    IsCompleted = false
                };

                var createdSet = await _apiService.CreateExerciseSetAsync(newSet);

                if (createdSet != null)
                {
                    Sets.Add(createdSet);
                    // Reset form
                    NewSetReps = 0;
                    NewSetWeight = 0;
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Failed to add set", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to add set: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        public async Task CompleteSetAsync(ExerciseSet set)
        {
            try
            {
                var success = await _apiService.CompleteExerciseSetAsync(set.Id);

                if (success)
                {
                    set.IsCompleted = true;
                    set.CompletedAt = DateTime.UtcNow;
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Failed to complete set", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to complete set: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        public async Task DeleteSetAsync(ExerciseSet set)
        {
            var confirm = await Application.Current.MainPage.DisplayAlert(
                "Delete Set",
                "Are you sure you want to delete this set?",
                "Yes",
                "No");

            if (!confirm) return;

            try
            {
                var success = await _apiService.DeleteExerciseSetAsync(set.Id);

                if (success)
                {
                    Sets.Remove(set);
                    // Renumber remaining sets
                    for (int i = 0; i < Sets.Count; i++)
                    {
                        Sets[i].SetNumber = i + 1;
                    }
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Failed to delete set", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to delete set: {ex.Message}", "OK");
            }
        }
    }
}
