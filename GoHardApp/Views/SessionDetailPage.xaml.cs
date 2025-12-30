using GoHardApp.ViewModels;

namespace GoHardApp.Views
{
    public partial class SessionDetailPage : ContentPage
    {
        public SessionDetailPage()
        {
            InitializeComponent();
            BindingContext = new SessionDetailViewModel();
        }
    }
}
