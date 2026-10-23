namespace Lab1.Core.Storage;

public sealed class SpreadsheetDocument
{
    public int Rows { get; set; }

    public int Columns { get; set; }

    public List<CellRecord> Cells { get; set; } = new();
}

public sealed class CellRecord
{
    public string Address { get; set; } = string.Empty;

    public string Expression { get; set; } = string.Empty;
}
