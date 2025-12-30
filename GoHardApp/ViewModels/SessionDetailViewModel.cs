using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoHardApp.Models;
using GoHardApp.Services;

namespace GoHardApp.ViewModels
{
    [QueryProperty(nameof(SessionId), "sessionId")]
    public partial class SessionDetailViewModel : ObservableObject
    {
        private readonly ApiService _apiService;

        [ObservableProperty]
        private int sessionId;

        [ObservableProperty]
        private Session? currentSession;

        [ObservableProperty]
        private bool isLoading;

        public SessionDetailViewModel()
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

                if (CurrentSession == null)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Session not found", "OK");
                    await Shell.Current.GoToAsync("..");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to load session: {ex.Message}", "OK");
                await Shell.Current.GoToAsync("..");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
