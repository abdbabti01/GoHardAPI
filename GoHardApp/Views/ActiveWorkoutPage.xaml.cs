using GoHardApp.ViewModels;

namespace GoHardApp.Views
{
    public partial class ActiveWorkoutPage : ContentPage
    {
        private ActiveWorkoutViewModel ViewModel => (ActiveWorkoutViewModel)BindingContext;

        public ActiveWorkoutPage()
        {
            InitializeComponent();
            BindingContext = new ActiveWorkoutViewModel();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Reload session data when page appears (e.g., after adding an exercise)
            if (ViewModel.SessionId > 0)
            {
                await ViewModel.LoadSessionCommand.ExecuteAsync(null);
            }
        }
    }
}
