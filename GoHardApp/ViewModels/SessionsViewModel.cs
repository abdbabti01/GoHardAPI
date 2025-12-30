using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoHardApp.Models;
using GoHardApp.Services;
using System.Collections.ObjectModel;

namespace GoHardApp.ViewModels
{
    public partial class SessionsViewModel : ObservableObject
    {
        private readonly ApiService _apiService;

        [ObservableProperty]
        private ObservableCollection<Session> sessions = new();

        [ObservableProperty]
        private bool isLoading;

        public SessionsViewModel()
        {
            _apiService = new ApiService();
        }

        [RelayCommand]
        public async Task LoadSessionsAsync()
        {
            if (IsLoading) return;

            IsLoading = true;
            try
            {
                var sessionList = await _apiService.GetSessionsAsync();
                Sessions.Clear();
                foreach (var session in sessionList.OrderByDescending(s => s.Date))
                {
                    Sessions.Add(session);
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to load sessions: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task DeleteSessionAsync(Session session)
        {
            var confirm = await Application.Current.MainPage.DisplayAlert(
                "Delete Session",
                "Are you sure you want to delete this workout session?",
                "Yes",
                "No");

            if (!confirm) return;

            IsLoading = true;
            try
            {
                var success = await _apiService.DeleteSessionAsync(session.Id);
                if (success)
                {
                    Sessions.Remove(session);
                    await Application.Current.MainPage.DisplayAlert("Success", "Session deleted successfully", "OK");
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Failed to delete session", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to delete session: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task StartNewWorkoutAsync()
        {
            try
            {
                // Get current user ID from AuthService
                var authService = new AuthService();
                var userId = await authService.GetUserIdAsync();

                if (userId == null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "User not authenticated", "OK");
                    return;
                }

                // Create a new draft session
                var newSession = new Session
                {
                    UserId = userId.Value,
                    Date = DateTime.UtcNow,
                    Type = "Workout",
                    Status = "draft",
                    Notes = ""
                };

                var createdSession = await _apiService.CreateSessionAsync(newSession);

                if (createdSession != null)
                {
                    // Navigate to active workout page with session ID
                    await Shell.Current.GoToAsync($"activeworkout?sessionId={createdSession.Id}");
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Failed to create workout session", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to start workout: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        public async Task ViewSessionAsync(Session session)
        {
            // If session is in progress or draft, navigate to active workout page
            // If session is completed, navigate to read-only detail page
            if (session.Status == "in_progress" || session.Status == "draft")
            {
                await Shell.Current.GoToAsync($"activeworkout?sessionId={session.Id}");
            }
            else
            {
                await Shell.Current.GoToAsync($"sessiondetail?sessionId={session.Id}");
            }
        }
    }
}
