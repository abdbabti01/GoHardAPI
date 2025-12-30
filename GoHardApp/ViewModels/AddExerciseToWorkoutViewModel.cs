using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoHardApp.Models;
using GoHardApp.Services;
using System.Collections.ObjectModel;

namespace GoHardApp.ViewModels
{
    [QueryProperty(nameof(SessionId), "sessionId")]
    public partial class AddExerciseToWorkoutViewModel : ObservableObject
    {
        private readonly ApiService _apiService;

        [ObservableProperty]
        private int sessionId;

        [ObservableProperty]
        private ObservableCollection<ExerciseTemplate> exerciseTemplates = new();

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string searchText = string.Empty;

        public AddExerciseToWorkoutViewModel()
        {
            _apiService = new ApiService();
        }

        partial void OnSessionIdChanged(int value)
        {
            if (value > 0)
            {
                LoadExerciseTemplatesCommand.Execute(null);
            }
        }

        [RelayCommand]
        public async Task LoadExerciseTemplatesAsync()
        {
            IsLoading = true;
            try
            {
                var templates = await _apiService.GetExerciseTemplatesAsync();
                ExerciseTemplates.Clear();
                foreach (var template in templates)
                {
                    ExerciseTemplates.Add(template);
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to load exercises: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task AddExerciseToWorkoutAsync(ExerciseTemplate template)
        {
            try
            {
                var exercise = await _apiService.AddExerciseToSessionAsync(SessionId, template.Id);

                if (exercise != null)
                {
                    // Navigate back to active workout page
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Failed to add exercise", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to add exercise: {ex.Message}", "OK");
            }
        }
    }
}
