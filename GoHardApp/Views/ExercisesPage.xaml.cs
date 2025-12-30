using GoHardApp.ViewModels;

namespace GoHardApp.Views
{
    public partial class ExercisesPage : ContentPage
    {
        public ExercisesPage()
        {
            InitializeComponent();
            BindingContext = new ExercisesViewModel();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            var viewModel = (ExercisesViewModel)BindingContext;
            await viewModel.LoadExercisesCommand.ExecuteAsync(null);
        }
    }
}
