using Lab1.Controls;
using Lab1.Core.Storage;
using Lab1.Core.Table;
using Lab1.Services;
using Cell = Lab1.Core.Table.Cell;
using Grid = Microsoft.Maui.Controls.Grid;

namespace Lab1;

public partial class MainPage : ContentPage
{
	private const double HeaderColumnWidth = 46;
	private const double MinCellWidth = 110;
	private const double HeaderRowHeight = 28;
	private const double RowHeight = 32;

	private readonly SpreadsheetFileService _fileService = new(new JsonSpreadsheetStorage());
	private readonly Dictionary<CellAddress, CellEntry> _cellEntries = new();
	private Spreadsheet _sheet = new();
	private CellAddress? _selectedAddress;
	private CellAddress? _formulaTarget;
	private double _cellWidth = MinCellWidth;
	private bool _showValues = true;
	private string _fileName = "таблиця.json";

	public MainPage()
	{
		InitializeComponent();
		BuildGrid();
		UpdateModeView();
		ShowStatus("Виберіть клітинку і введіть вираз, наприклад: (A1 + 5) * 2. Допомога - кнопка «Допомога» або меню «Інформація».");
	}

	private void BuildGrid()
	{
		grid.Children.Clear();
		grid.RowDefinitions.Clear();
		grid.ColumnDefinitions.Clear();
		_cellEntries.Clear();
		ClearSelection();

		grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(HeaderColumnWidth)));
		grid.RowDefinitions.Add(new RowDefinition(new GridLength(HeaderRowHeight)));
		grid.Add(CreateHeaderLabel(string.Empty), 0, 0);

		for (var column = 0; column < _sheet.ColumnCount; column++)
		{
			grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(_cellWidth)));
			grid.Add(CreateHeaderLabel(CellAddress.GetColumnName(column)), column + 1, 0);
		}

		for (var row = 0; row < _sheet.RowCount; row++)
		{
			AddGridRow(row);
		}

		UpdateColumnWidths();
		RefreshCells();
	}

	private void GridScroll_SizeChanged(object? sender, EventArgs e) => UpdateColumnWidths();

	private void UpdateColumnWidths()
	{
		var available = gridScroll.Width - HeaderColumnWidth - _sheet.ColumnCount * grid.ColumnSpacing;
		if (available <= 0)
		{
			return;
		}

		_cellWidth = Math.Max(MinCellWidth, Math.Floor(available / _sheet.ColumnCount));
		foreach (var column in grid.ColumnDefinitions.Skip(1))
		{
			if (column.Width.Value != _cellWidth)
			{
				column.Width = new GridLength(_cellWidth);
			}
		}
	}

	private void AddGridRow(int row)
	{
		grid.RowDefinitions.Add(new RowDefinition(new GridLength(RowHeight)));
		grid.Add(CreateHeaderLabel((row + 1).ToString()), 0, row + 1);

		for (var column = 0; column < _sheet.ColumnCount; column++)
		{
			AddCellEntry(new CellAddress(row, column));
		}
	}

	private void AddGridColumn(int column)
	{
		grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(_cellWidth)));
		grid.Add(CreateHeaderLabel(CellAddress.GetColumnName(column)), column + 1, 0);

		for (var row = 0; row < _sheet.RowCount; row++)
		{
			AddCellEntry(new CellAddress(row, column));
		}

		UpdateColumnWidths();
	}

	private void AddCellEntry(CellAddress address)
	{
		var entry = CreateCellEntry(address);
		_cellEntries[address] = entry;

		var container = new ContentView
		{
			BackgroundColor = AppColors.Surface,
			Padding = new Thickness(6, 0),
			Content = entry
		};
		grid.Add(container, address.Column + 1, address.Row + 1);
	}

	private void RemoveGridRow(int row)
	{
		RemoveGridChildren(view => Grid.GetRow(view) == row + 1);
		grid.RowDefinitions.RemoveAt(row + 1);
		RemoveCellEntries(address => address.Row == row);
	}

	private void RemoveGridColumn(int column)
	{
		RemoveGridChildren(view => Grid.GetColumn(view) == column + 1);
		grid.ColumnDefinitions.RemoveAt(column + 1);
		RemoveCellEntries(address => address.Column == column);
		UpdateColumnWidths();
	}

	private void RemoveGridChildren(Func<View, bool> isRemoved)
	{
		foreach (var view in grid.Children.OfType<View>().Where(isRemoved).ToList())
		{
			grid.Children.Remove(view);
		}
	}

	private void RemoveCellEntries(Func<CellAddress, bool> isRemoved)
	{
		foreach (var address in _cellEntries.Keys.Where(isRemoved).ToList())
		{
			_cellEntries.Remove(address);
		}

		if (_selectedAddress is { } selected && isRemoved(selected))
		{
			ClearSelection();
		}
	}

	private void ClearSelection()
	{
		var previous = _selectedAddress;
		_selectedAddress = null;
		_formulaTarget = null;
		selectedCellLabel.Text = "-";
		textInput.Text = string.Empty;

		if (previous is { } address)
		{
			UpdateCellAppearance(address);
		}
	}

	private static Label CreateHeaderLabel(string text) => new()
	{
		Text = text,
		FontSize = 12,
		FontAttributes = FontAttributes.Bold,
		TextColor = AppColors.HeaderText,
		BackgroundColor = AppColors.HeaderBack,
		HorizontalTextAlignment = TextAlignment.Center,
		VerticalTextAlignment = TextAlignment.Center
	};

	private CellEntry CreateCellEntry(CellAddress address)
	{
		var entry = new CellEntry
		{
			FontSize = 14,
			TextColor = AppColors.Ink,
			BackgroundColor = Colors.Transparent,
			MinimumHeightRequest = 0,
			VerticalOptions = LayoutOptions.Center
		};
		entry.Focused += (_, _) => OnCellFocused(address, entry);
		entry.Unfocused += (_, _) => OnCellUnfocused(address, entry);
		entry.Completed += (_, _) => entry.Unfocus();
		return entry;
	}

	private void OnCellFocused(CellAddress address, CellEntry entry)
	{
		if (!IsCurrentEntry(address, entry))
		{
			return;
		}

		var previous = _selectedAddress;
		var cell = _sheet.GetCell(address);
		_selectedAddress = address;
		selectedCellLabel.Text = address.ToString();
		textInput.Text = cell.Expression;
		entry.Text = cell.Expression;

		if (previous is { } previousAddress && previousAddress != address)
		{
			UpdateCellAppearance(previousAddress);
		}
		UpdateCellAppearance(address);
		ShowCellStatus(cell);
	}

	private void OnCellUnfocused(CellAddress address, CellEntry entry)
	{
		if (IsCurrentEntry(address, entry))
		{
			CommitExpression(address, entry.Text ?? string.Empty);
		}
	}

	private bool IsCurrentEntry(CellAddress address, CellEntry entry) =>
		_cellEntries.TryGetValue(address, out var current) && ReferenceEquals(current, entry);

	private void TextInput_Completed(object? sender, EventArgs e)
	{
		if (_selectedAddress is not { } address)
		{
			ShowStatus("Спочатку виберіть клітинку в таблиці", isError: true);
			return;
		}
		CommitExpression(address, textInput.Text ?? string.Empty);
	}

	private void TextInput_Focused(object? sender, FocusEventArgs e) =>
		_formulaTarget = _selectedAddress;

	private void TextInput_Unfocused(object? sender, FocusEventArgs e)
	{
		if (_formulaTarget is { } address && _sheet.Contains(address))
		{
			CommitExpression(address, textInput.Text ?? string.Empty);
		}
		_formulaTarget = null;
	}

	private void CommitExpression(CellAddress address, string expression)
	{
		_sheet.SetExpression(address, expression);
		RefreshCells();

		var cell = _sheet.GetCell(address);
		if (_selectedAddress == address)
		{
			textInput.Text = cell.Expression;
		}
		ShowCellStatus(cell);
	}

	private void CommitPendingEdit()
	{
		if (textInput.IsFocused && _formulaTarget is { } target && _sheet.Contains(target))
		{
			CommitExpression(target, textInput.Text ?? string.Empty);
			return;
		}

		foreach (var (address, entry) in _cellEntries)
		{
			if (entry.IsFocused)
			{
				CommitExpression(address, entry.Text ?? string.Empty);
				entry.Unfocus();
				return;
			}
		}
	}

	private void RefreshCells()
	{
		foreach (var (address, entry) in _cellEntries)
		{
			if (!entry.IsFocused)
			{
				var text = _sheet.GetCell(address).GetDisplayText(_showValues);
				if (entry.Text != text)
				{
					entry.Text = text;
				}
			}
			UpdateCellAppearance(address);
		}
	}

	private void UpdateCellAppearance(CellAddress address)
	{
		if (!_cellEntries.TryGetValue(address, out var entry) || entry.Parent is not ContentView container)
		{
			return;
		}

		var cell = _sheet.GetCell(address);
		var background = address == _selectedAddress ? AppColors.AccentSoft
			: cell.HasError ? AppColors.ErrorBack
			: AppColors.Surface;
		var textColor = cell.HasError && !entry.IsFocused ? AppColors.ErrorText : AppColors.Ink;

		if (container.BackgroundColor != background)
		{
			container.BackgroundColor = background;
		}
		if (entry.TextColor != textColor)
		{
			entry.TextColor = textColor;
		}
	}

	private void ShowCellStatus(Cell cell)
	{
		if (cell.HasError)
		{
			ShowStatus($"{cell.Address}: {cell.Error}", isError: true);
		}
		else if (cell.IsEmpty)
		{
			ShowStatus($"{cell.Address}: порожня клітинка");
		}
		else
		{
			ShowStatus($"{cell.Address}: {cell.Expression} = {cell.Value}");
		}
	}

	private void ShowStatus(string message, bool isError = false)
	{
		statusLabel.Text = isError ? "⚠ " + message : message;
		statusLabel.TextColor = isError ? AppColors.StatusError : AppColors.StatusText;
	}

	private void UpdateModeView()
	{
		SetSegmentState(expressionModeButton, isActive: !_showValues);
		SetSegmentState(valueModeButton, isActive: _showValues);
		RefreshCells();
	}

	private static void SetSegmentState(Button button, bool isActive)
	{
		button.BackgroundColor = isActive ? AppColors.Accent : Colors.Transparent;
		button.TextColor = isActive ? AppColors.Surface : AppColors.AccentDark;
	}

	private void SetMode(bool showValues)
	{
		CommitPendingEdit();
		_showValues = showValues;
		UpdateModeView();
		ShowStatus(showValues ? "Режим ЗНАЧЕННЯ: у клітинках показано результати" : "Режим ВИРАЗ: у клітинках показано введені вирази");
	}

	private void ExpressionMode_Clicked(object? sender, EventArgs e) => SetMode(false);

	private void ValueMode_Clicked(object? sender, EventArgs e) => SetMode(true);

	private void CalculateButton_Clicked(object? sender, EventArgs e)
	{
		CommitPendingEdit();
		_sheet.Recalculate();
		_showValues = true;
		UpdateModeView();

		var errorCount = _sheet.Cells.Count(cell => cell.HasError);
		ShowStatus(errorCount == 0
				? "Таблицю перераховано, помилок немає"
				: $"Таблицю перераховано. Клітинок з помилками: {errorCount} (виберіть клітинку, щоб побачити опис)",
			isError: errorCount > 0);
	}

	private async void AddRowButton_Clicked(object? sender, EventArgs e) =>
		await ChangeStructureAsync(_sheet.AddRow, () => AddGridRow(_sheet.RowCount - 1), null);

	private async void AddColumnButton_Clicked(object? sender, EventArgs e) =>
		await ChangeStructureAsync(_sheet.AddColumn, () => AddGridColumn(_sheet.ColumnCount - 1), null);

	private async void DeleteRowButton_Clicked(object? sender, EventArgs e)
	{
		CommitPendingEdit();
		var row = _sheet.RowCount - 1;
		await ChangeStructureAsync(_sheet.RemoveLastRow, () => RemoveGridRow(row),
			_sheet.IsLastRowInUse() ? $"Рядок {row + 1} містить дані або на нього посилаються інші клітинки. Видалити?" : null);
	}

	private async void DeleteColumnButton_Clicked(object? sender, EventArgs e)
	{
		CommitPendingEdit();
		var column = _sheet.ColumnCount - 1;
		await ChangeStructureAsync(_sheet.RemoveLastColumn, () => RemoveGridColumn(column),
			_sheet.IsLastColumnInUse() ? $"Стовпчик {CellAddress.GetColumnName(column)} містить дані або на нього посилаються інші клітинки. Видалити?" : null);
	}

	private async Task ChangeStructureAsync(Action change, Action updateGrid, string? confirmation)
	{
		CommitPendingEdit();

		if (confirmation is not null &&
			!await DisplayAlertAsync("Підтвердження", confirmation, "Так", "Ні"))
		{
			return;
		}

		try
		{
			change();
			updateGrid();
			RefreshCells();
			ShowStatus($"Розмір таблиці: {_sheet.RowCount} рядків x {_sheet.ColumnCount} стовпчиків");
		}
		catch (InvalidOperationException ex)
		{
			await DisplayAlertAsync("Неможливо виконати", ex.Message, "Гаразд");
		}
	}

	private async void NewButton_Clicked(object? sender, EventArgs e)
	{
		CommitPendingEdit();
		if (!await ConfirmDiscardChangesAsync())
		{
			return;
		}

		_sheet = new Spreadsheet();
		_fileName = "таблиця.json";
		BuildGrid();
		ShowStatus("Створено нову таблицю");
	}

	private async void SaveButton_Clicked(object? sender, EventArgs e)
	{
		CommitPendingEdit();

		try
		{
			var path = await _fileService.SaveAsync(_sheet, _fileName);
			if (path is null)
			{
				ShowStatus("Файл не збережено");
				return;
			}

			_fileName = Path.GetFileName(path);
			ShowStatus($"Таблицю збережено у файл {path}");
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			await DisplayAlertAsync("Помилка збереження", ex.Message, "Гаразд");
		}
	}

	private async void ReadButton_Clicked(object? sender, EventArgs e)
	{
		CommitPendingEdit();
		if (!await ConfirmDiscardChangesAsync())
		{
			return;
		}

		try
		{
			var opened = await _fileService.OpenAsync();
			if (opened is null)
			{
				return;
			}

			_sheet = opened.Value.Sheet;
			_fileName = opened.Value.FileName;
			BuildGrid();
			ShowStatus($"Відкрито файл {_fileName}");
		}
		catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException)
		{
			await DisplayAlertAsync("Помилка відкриття файлу", ex.Message, "Гаразд");
		}
	}

	private async void ExitButton_Clicked(object? sender, EventArgs e)
	{
		CommitPendingEdit();

		var message = _sheet.IsModified
			? "У таблиці є незбережені зміни. Ви дійсно хочете вийти?"
			: "Ви дійсно хочете вийти?";
		if (await DisplayAlertAsync("Підтвердження", message, "Так", "Ні"))
		{
			Application.Current?.Quit();
		}
	}

	private async void HelpButton_Clicked(object? sender, EventArgs e) =>
		await Navigation.PushModalAsync(new HelpPage());

	private async void AboutButton_Clicked(object? sender, EventArgs e) =>
		await DisplayAlertAsync("Про програму", HelpContent.About, "Гаразд");

	private async Task<bool> ConfirmDiscardChangesAsync() =>
		!_sheet.IsModified ||
		await DisplayAlertAsync("Незбережені зміни",
			"У таблиці є незбережені зміни. Продовжити без збереження?", "Так", "Ні");
}
