using System.Text.Encodings.Web;
using System.Text.Json;
using Lab1.Core.Table;

namespace Lab1.Core.Storage;

public sealed class JsonSpreadsheetStorage : ISpreadsheetStorage
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public void Save(Spreadsheet sheet, Stream stream)
    {
        var document = new SpreadsheetDocument
        {
            Rows = sheet.RowCount,
            Columns = sheet.ColumnCount,
            Cells = sheet.Cells
                .Where(cell => !cell.IsEmpty)
                .Select(cell => new CellRecord { Address = cell.Address.ToString(), Expression = cell.Expression })
                .ToList()
        };

        JsonSerializer.Serialize(stream, document, Options);
    }

    public Spreadsheet Load(Stream stream)
    {
        SpreadsheetDocument? document;
        try
        {
            document = JsonSerializer.Deserialize<SpreadsheetDocument>(stream, Options);
        }
        catch (JsonException)
        {
            throw new InvalidDataException("Файл пошкоджено або він не є таблицею цієї програми");
        }

        if (document is null)
        {
            throw new InvalidDataException("Файл порожній");
        }
        if (document.Rows < 1 || document.Rows > Spreadsheet.MaxRowCount ||
            document.Columns < 1 || document.Columns > Spreadsheet.MaxColumnCount)
        {
            throw new InvalidDataException($"Некоректний розмір таблиці у файлі: {document.Rows} x {document.Columns}");
        }

        var sheet = new Spreadsheet(document.Rows, document.Columns);
        foreach (var record in document.Cells ?? [])
        {
            if (!CellAddress.TryParse(record.Address, out var address) || !sheet.Contains(address.Value))
            {
                throw new InvalidDataException($"Некоректна адреса клітинки у файлі: '{record.Address}'");
            }
            sheet.LoadExpression(address.Value, record.Expression ?? string.Empty);
        }

        sheet.Recalculate();
        return sheet;
    }
}
