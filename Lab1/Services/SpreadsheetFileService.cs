using CommunityToolkit.Maui.Storage;
using Lab1.Core.Storage;
using Lab1.Core.Table;

namespace Lab1.Services;

public sealed class SpreadsheetFileService
{
	private static readonly FilePickerFileType TableFileType = new(new Dictionary<DevicePlatform, IEnumerable<string>>
	{
		[DevicePlatform.MacCatalyst] = ["public.json"],
		[DevicePlatform.WinUI] = [".json"]
	});

	private readonly ISpreadsheetStorage _storage;

	public SpreadsheetFileService(ISpreadsheetStorage storage)
	{
		_storage = storage;
	}

	public async Task<string?> SaveAsync(Spreadsheet sheet, string suggestedFileName)
	{
		using var stream = new MemoryStream();
		_storage.Save(sheet, stream);
		stream.Position = 0;

		var result = await FileSaver.Default.SaveAsync(suggestedFileName, stream, CancellationToken.None);
		if (!result.IsSuccessful || result.FilePath is null)
		{
			return null;
		}

		sheet.MarkSaved();
		return result.FilePath;
	}

	public async Task<(Spreadsheet Sheet, string FileName)?> OpenAsync()
	{
		var file = await FilePicker.Default.PickAsync(new PickOptions
		{
			PickerTitle = "Виберіть файл таблиці",
			FileTypes = TableFileType
		});
		if (file is null)
		{
			return null;
		}

		await using var stream = await file.OpenReadAsync();
		return (_storage.Load(stream), file.FileName);
	}
}
