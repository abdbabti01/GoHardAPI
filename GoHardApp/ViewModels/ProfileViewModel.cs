using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoHardApp.Services;

namespace GoHardApp.ViewModels
{
    public partial class ProfileViewModel : ObservableObject
    {
        private readonly AuthService _authService;

        [ObservableProperty]
        private string userName = "";

        [ObservableProperty]
        private string userEmail = "";

        public ProfileViewModel()
        {
            _authService = new AuthService();
        }

        [RelayCommand]
        public async Task LoadUserInfoAsync()
        {
            try
            {
                UserName = await _authService.GetUserNameAsync() ?? "Unknown";
                UserEmail = await _authService.GetUserEmailAsync() ?? "Unknown";
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to load user info: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        public async Task LogoutAsync()
        {
            var confirm = await Application.Current.MainPage.DisplayAlert(
                "Logout",
                "Are you sure you want to logout?",
                "Yes",
                "No");

            if (!confirm) return;

            try
            {
                // Clear stored token and user info
                _authService.ClearToken();

                // Navigate to login page
                await Shell.Current.GoToAsync("//login");

                await Application.Current.MainPage.DisplayAlert("Success", "Logged out successfully", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Logout failed: {ex.Message}", "OK");
            }
        }
    }
}
