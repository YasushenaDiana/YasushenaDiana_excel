namespace Lab1;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
		UserAppTheme = AppTheme.Light;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell())
		{
			Title = "YasushenaDiana_excel",
			Width = 1280,
			Height = 820
		};
	}
}
