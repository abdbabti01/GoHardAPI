using GoHardApp.Services;

namespace GoHardApp;

public partial class App : Application
{
	private readonly AuthService _authService;

	public App()
	{
		InitializeComponent();
		_authService = new AuthService();

		MainPage = new AppShell();
	}

	protected override async void OnStart()
	{
		base.OnStart();

		// Check if user is authenticated
		var isAuthenticated = await _authService.IsAuthenticatedAsync();

		if (!isAuthenticated)
		{
			// Navigate to login page
			await Shell.Current.GoToAsync("//login");
		}
	}
}
