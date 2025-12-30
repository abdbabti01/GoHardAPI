using GoHardApp.Views;

namespace GoHardApp;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Register navigation routes
		Routing.RegisterRoute("exercisedetail", typeof(ExerciseDetailPage));
		Routing.RegisterRoute("login", typeof(LoginPage));
		Routing.RegisterRoute("signup", typeof(SignupPage));
		Routing.RegisterRoute("activeworkout", typeof(ActiveWorkoutPage));
		Routing.RegisterRoute("addexercise", typeof(AddExerciseToWorkoutPage));
		Routing.RegisterRoute("logsets", typeof(LogSetsPage));
		Routing.RegisterRoute("sessiondetail", typeof(SessionDetailPage));
	}
}
