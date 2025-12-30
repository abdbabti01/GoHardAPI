using GoHardApp.ViewModels;

namespace GoHardApp.Views
{
    public partial class SessionsPage : ContentPage
    {
        public SessionsPage()
        {
            InitializeComponent();
            BindingContext = new SessionsViewModel();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            var viewModel = (SessionsViewModel)BindingContext;
            await viewModel.LoadSessionsCommand.ExecuteAsync(null);
        }
    }
}
