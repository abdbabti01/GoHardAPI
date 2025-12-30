using GoHardApp.ViewModels;

namespace GoHardApp.Views
{
    public partial class SignupPage : ContentPage
    {
        public SignupPage()
        {
            InitializeComponent();
            BindingContext = new SignupViewModel();
        }
    }
}
