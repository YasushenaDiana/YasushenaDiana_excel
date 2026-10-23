using Lab1.Core.Expressions;

namespace Lab1.Core.Table;

public sealed class Spreadsheet
{
    public const int DefaultRowCount = 20;
    public const int DefaultColumnCount = 10;
    public const int MaxRowCount = 100;
    public const int MaxColumnCount = 26;

    private readonly List<List<Cell>> _rows = new();
    private readonly Calculator _calculator;

    public Spreadsheet(int rowCount = DefaultRowCount, int columnCount = DefaultColumnCount,
        Calculator? calculator = null)
    {
        if (rowCount < 1 || rowCount > MaxRowCount)
        {
            throw new ArgumentOutOfRangeException(nameof(rowCount), $"Кількість рядків має бути від 1 до {MaxRowCount}");
        }
        if (columnCount < 1 || columnCount > MaxColumnCount)
        {
            throw new ArgumentOutOfRangeException(nameof(columnCount), $"Кількість стовпчиків має бути від 1 до {MaxColumnCount}");
        }

        _calculator = calculator ?? new Calculator();
        ColumnCount = columnCount;
        for (var row = 0; row < rowCount; row++)
        {
            _rows.Add(CreateRow(row));
        }
    }

    public int RowCount => _rows.Count;

    public int ColumnCount { get; private set; }

    public bool IsModified { get; private set; }

    public IEnumerable<Cell> Cells => _rows.SelectMany(row => row);

    public bool Contains(CellAddress address) =>
        address.Row >= 0 && address.Row < RowCount && address.Column >= 0 && address.Column < ColumnCount;

    public Cell GetCell(CellAddress address)
    {
        if (!Contains(address))
        {
            throw new ArgumentOutOfRangeException(nameof(address), $"Клітинки {address} немає в таблиці");
        }
        return _rows[address.Row][address.Column];
    }

    public void SetExpression(CellAddress address, string expression)
    {
        var cell = GetCell(address);
        if (cell.Expression == expression.Trim())
        {
            return;
        }

        cell.SetExpression(expression, _calculator);
        IsModified = true;
        Recalculate();
    }

    internal void LoadExpression(CellAddress address, string expression) =>
        GetCell(address).SetExpression(expression, _calculator);

    public void Recalculate() => new SpreadsheetEvaluator(this, _calculator).EvaluateAll();

    public void AddRow()
    {
        if (RowCount >= MaxRowCount)
        {
            throw new InvalidOperationException($"Досягнуто максимальної кількості рядків ({MaxRowCount})");
        }

        _rows.Add(CreateRow(RowCount));
        IsModified = true;
        Recalculate();
    }

    public void AddColumn()
    {
        if (ColumnCount >= MaxColumnCount)
        {
            throw new InvalidOperationException($"Досягнуто максимальної кількості стовпчиків ({MaxColumnCount})");
        }

        for (var row = 0; row < RowCount; row++)
        {
            _rows[row].Add(new Cell(new CellAddress(row, ColumnCount)));
        }
        ColumnCount++;
        IsModified = true;
        Recalculate();
    }

    public void RemoveLastRow()
    {
        if (RowCount <= 1)
        {
            throw new InvalidOperationException("У таблиці має залишитися хоча б один рядок");
        }

        _rows.RemoveAt(RowCount - 1);
        IsModified = true;
        Recalculate();
    }

    public void RemoveLastColumn()
    {
        if (ColumnCount <= 1)
        {
            throw new InvalidOperationException("У таблиці має залишитися хоча б один стовпчик");
        }

        foreach (var row in _rows)
        {
            row.RemoveAt(ColumnCount - 1);
        }
        ColumnCount--;
        IsModified = true;
        Recalculate();
    }

    public bool IsLastRowInUse() => IsInUse(address => address.Row == RowCount - 1);

    public bool IsLastColumnInUse() => IsInUse(address => address.Column == ColumnCount - 1);

    public void MarkSaved() => IsModified = false;

    private bool IsInUse(Func<CellAddress, bool> isRemoved) =>
        Cells.Any(cell =>
            (isRemoved(cell.Address) && !cell.IsEmpty) ||
            (!isRemoved(cell.Address) && cell.References.Any(isRemoved)));

    private List<Cell> CreateRow(int row) =>
        Enumerable.Range(0, ColumnCount)
            .Select(column => new Cell(new CellAddress(row, column)))
            .ToList();
}
