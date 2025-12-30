using GoHardApp.ViewModels;

namespace GoHardApp.Views
{
    public partial class ExerciseDetailPage : ContentPage
    {
        public ExerciseDetailPage()
        {
            InitializeComponent();
            BindingContext = new ExerciseDetailViewModel();
        }
    }
}
