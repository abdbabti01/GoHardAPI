using GoHardApp.ViewModels;

namespace GoHardApp.Views
{
    public partial class LogSetsPage : ContentPage
    {
        public LogSetsPage()
        {
            InitializeComponent();
            BindingContext = new LogSetsViewModel();
        }
    }
}
