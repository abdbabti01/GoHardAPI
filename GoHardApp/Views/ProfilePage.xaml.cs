using GoHardApp.ViewModels;

namespace GoHardApp.Views
{
    public partial class ProfilePage : ContentPage
    {
        private ProfileViewModel ViewModel => (ProfileViewModel)BindingContext;

        public ProfilePage()
        {
            InitializeComponent();
            BindingContext = new ProfileViewModel();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await ViewModel.LoadUserInfoCommand.ExecuteAsync(null);
        }
    }
}
