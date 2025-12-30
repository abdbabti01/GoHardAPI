using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoHardApp.Models;
using GoHardApp.Services;
using Microsoft.Maui.Graphics;

namespace GoHardApp.ViewModels
{
    [QueryProperty(nameof(ExerciseId), "id")]
    public partial class ExerciseDetailViewModel : ObservableObject
    {
        private readonly ApiService _apiService;

        [ObservableProperty]
        private ExerciseTemplate? exercise;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private int exerciseId;

        public ExerciseDetailViewModel()
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
            if (ExerciseId <= 0) return;

            IsLoading = true;
            try
            {
                Exercise = await _apiService.GetExerciseTemplateAsync(ExerciseId);

                if (Exercise == null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Exercise not found", "OK");
                    await Shell.Current.GoToAsync("..");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to load exercise: {ex.Message}", "OK");
                await Shell.Current.GoToAsync("..");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public bool HasImage => Exercise?.ImageUrl != null;
        public bool HasVideoUrl => Exercise?.VideoUrl != null;
        public bool HasInstructions => !string.IsNullOrWhiteSpace(Exercise?.Instructions);
        public Color CategoryColor
        {
            get
            {
                if (Exercise?.Category == null) return Colors.Gray;

                var category = Exercise.Category.ToLower();
                return category switch
                {
                    "strength" => Color.FromArgb("#E53935"),
                    "cardio" => Color.FromArgb("#1E88E5"),
                    "flexibility" => Color.FromArgb("#43A047"),
                    "core" => Color.FromArgb("#FB8C00"),
                    _ => Color.FromArgb("#9E9E9E")
                };
            }
        }

        public string CategoryIcon
        {
            get
            {
                if (Exercise?.Category == null) return "🏋️";

                var category = Exercise.Category.ToLower();
                return category switch
                {
                    "strength" => "💪",
                    "cardio" => "❤️",
                    "flexibility" => "🧘",
                    "core" => "⚡",
                    _ => "🏋️"
                };
            }
        }
    }
}
