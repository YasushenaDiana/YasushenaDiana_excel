namespace Lab1;

public partial class HelpPage : ContentPage
{
	public HelpPage()
	{
		InitializeComponent();
		helpLabel.Text = HelpContent.Help;
	}

	private async void CloseButton_Clicked(object? sender, EventArgs e) =>
		await Navigation.PopModalAsync();
}
