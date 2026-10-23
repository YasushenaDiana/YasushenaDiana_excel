using System.Numerics;
using Lab1.Core.Expressions;

namespace Lab1.Core.Table;

internal sealed class SpreadsheetEvaluator : ICellValueProvider
{
    private enum State
    {
        InProgress,
        Done
    }

    private readonly Spreadsheet _sheet;
    private readonly Calculator _calculator;
    private readonly Dictionary<CellAddress, State> _states = new();

    public SpreadsheetEvaluator(Spreadsheet sheet, Calculator calculator)
    {
        _sheet = sheet;
        _calculator = calculator;
    }

    public void EvaluateAll()
    {
        foreach (var cell in _sheet.Cells)
        {
            try
            {
                Evaluate(cell);
            }
            catch (CircularReferenceException)
            {
            }
        }
    }

    public BigInteger GetCellValue(CellAddress address)
    {
        if (!_sheet.Contains(address))
        {
            throw new ExpressionEvaluationException($"Клітинки {address} немає в таблиці");
        }

        var cell = _sheet.GetCell(address);
        Evaluate(cell);

        if (cell.HasError)
        {
            throw new ExpressionEvaluationException($"Клітинка {address} містить помилку");
        }
        return cell.Value ?? BigInteger.Zero;
    }

    private void Evaluate(Cell cell)
    {
        if (_states.TryGetValue(cell.Address, out var state))
        {
            if (state == State.InProgress)
            {
                throw new CircularReferenceException(cell.Address);
            }
            return;
        }

        _states[cell.Address] = State.InProgress;
        cell.Value = null;
        cell.Error = cell.SyntaxError;

        try
        {
            if (cell.ParsedExpression is not null)
            {
                cell.Value = _calculator.Evaluate(cell.ParsedExpression, this);
            }
        }
        catch (CircularReferenceException ex)
        {
            cell.Error = ex.Message;
            _states[cell.Address] = State.Done;
            throw;
        }
        catch (ExpressionEvaluationException ex)
        {
            cell.Error = ex.Message;
        }

        _states[cell.Address] = State.Done;
    }
}
