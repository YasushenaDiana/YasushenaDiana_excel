using Foundation;
using UIKit;

namespace Lab1;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

	public override void BuildMenu(IUIMenuBuilder builder)
	{
		base.BuildMenu(builder);

		foreach (var menu in new[] { UIMenuIdentifier.File, UIMenuIdentifier.Format, UIMenuIdentifier.View })
		{
			builder.RemoveMenu(menu.GetConstant()!);
		}
	}
}
