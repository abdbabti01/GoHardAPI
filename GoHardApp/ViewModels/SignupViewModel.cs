using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoHardApp.Models;
using GoHardApp.Services;

namespace GoHardApp.ViewModels
{
    public partial class SignupViewModel : ObservableObject
    {
        private readonly ApiService _apiService;

        [ObservableProperty]
        private string name = string.Empty;

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private string confirmPassword = string.Empty;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        public SignupViewModel()
        {
            _apiService = new ApiService();
        }

        [RelayCommand]
        public async Task SignupAsync()
        {
            // Validation
            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "Please enter your name";
                return;
            }

            if (string.IsNullOrWhiteSpace(Email))
            {
                ErrorMessage = "Please enter your email";
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Please enter a password";
                return;
            }

            if (Password.Length < 6)
            {
                ErrorMessage = "Password must be at least 6 characters";
                return;
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Passwords do not match";
                return;
            }

            IsLoading = true;
            ErrorMessage = string.Empty;

            try
            {
                var request = new SignupRequest
                {
                    Name = Name,
                    Email = Email,
                    Password = Password
                };

                var response = await _apiService.SignupAsync(request);

                if (response != null)
                {
                    // Navigate to main app
                    await Shell.Current.GoToAsync("//main");
                }
                else
                {
                    ErrorMessage = "Signup failed. Email may already be registered.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Signup failed: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Signup error: {ex}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task NavigateToLoginAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
