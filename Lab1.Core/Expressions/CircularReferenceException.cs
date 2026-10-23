using Lab1.Core.Table;

namespace Lab1.Core.Expressions;

public sealed class CircularReferenceException : ExpressionEvaluationException
{
    public CircularReferenceException(CellAddress address)
        : base($"Циклічне посилання через клітинку {address}")
    {
        Address = address;
    }

    public CellAddress Address { get; }
}
