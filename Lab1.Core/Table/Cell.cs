using System.Numerics;
using Lab1.Core.Expressions;

namespace Lab1.Core.Table;

public sealed class Cell
{
    internal Cell(CellAddress address)
    {
        Address = address;
    }

    public CellAddress Address { get; }

    public string Expression { get; private set; } = string.Empty;

    public BigInteger? Value { get; internal set; }

    public string? Error { get; internal set; }

    public bool IsEmpty => string.IsNullOrWhiteSpace(Expression);

    public bool HasError => Error is not null;

    public IReadOnlySet<CellAddress> References => ParsedExpression?.References ?? EmptyReferences;

    internal ParsedExpression? ParsedExpression { get; private set; }

    internal string? SyntaxError { get; private set; }

    private static readonly IReadOnlySet<CellAddress> EmptyReferences = new HashSet<CellAddress>();

    internal void SetExpression(string expression, Calculator calculator)
    {
        Expression = expression.Trim();
        ParsedExpression = null;
        SyntaxError = null;

        if (IsEmpty)
        {
            return;
        }

        try
        {
            ParsedExpression = calculator.Parse(Expression);
        }
        catch (ExpressionSyntaxException ex)
        {
            SyntaxError = ex.Message;
        }
    }

    public string GetDisplayText(bool showValues)
    {
        if (!showValues)
        {
            return Expression;
        }
        if (HasError)
        {
            return "#ПОМИЛКА";
        }
        return Value?.ToString() ?? string.Empty;
    }
}
