using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoHardApp.Models;
using GoHardApp.Services;
using System.Collections.ObjectModel;

namespace GoHardApp.ViewModels
{
    public partial class ExercisesViewModel : ObservableObject
    {
        private readonly ApiService _apiService;

        public ObservableCollection<ExerciseTemplate> Exercises { get; } = new();

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string selectedCategory = "All";

        public ExercisesViewModel()
        {
            _apiService = new ApiService();
        }

        [RelayCommand]
        public async Task LoadExercisesAsync()
        {
            if (IsLoading) return;

            IsLoading = true;
            try
            {
                var exerciseList = await _apiService.GetExerciseTemplatesAsync();

                Exercises.Clear();
                foreach (var exercise in exerciseList)
                {
                    Exercises.Add(exercise);
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
        public async Task FilterByCategoryAsync(string category)
        {
            if (IsLoading) return;

            SelectedCategory = category;
            IsLoading = true;
            try
            {
                var exerciseList = category == "All"
                    ? await _apiService.GetExerciseTemplatesAsync()
                    : await _apiService.GetExerciseTemplatesAsync(category: category);

                Exercises.Clear();
                foreach (var exercise in exerciseList)
                {
                    Exercises.Add(exercise);
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to filter exercises: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task FilterByMuscleGroupAsync(string muscleGroup)
        {
            if (IsLoading) return;

            IsLoading = true;
            try
            {
                var exerciseList = await _apiService.GetExerciseTemplatesAsync(muscleGroup: muscleGroup);
                Exercises.Clear();
                foreach (var exercise in exerciseList)
                {
                    Exercises.Add(exercise);
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to filter exercises: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task NavigateToDetailAsync(ExerciseTemplate exercise)
        {
            if (exercise == null) return;

            await Shell.Current.GoToAsync($"exercisedetail?id={exercise.Id}");
        }
    }
}
