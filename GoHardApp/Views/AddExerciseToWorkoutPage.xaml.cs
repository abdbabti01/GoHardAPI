using GoHardApp.ViewModels;

namespace GoHardApp.Views
{
    public partial class AddExerciseToWorkoutPage : ContentPage
    {
        public AddExerciseToWorkoutPage()
        {
            InitializeComponent();
            BindingContext = new AddExerciseToWorkoutViewModel();
        }
    }
}
