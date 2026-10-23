using CommunityToolkit.Maui;
using Lab1.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;

namespace Lab1;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		EntryHandler.Mapper.AppendToMapping("FlatCellEntry", (handler, view) =>
		{
			if (view is not CellEntry)
			{
				return;
			}
#if MACCATALYST
			handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
#elif WINDOWS
			handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
#endif
		});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
