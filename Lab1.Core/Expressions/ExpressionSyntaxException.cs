namespace Lab1.Core.Expressions;

public sealed class ExpressionSyntaxException : ExpressionException
{
    public ExpressionSyntaxException(int position, string description)
        : base($"Синтаксична помилка (позиція {position}): {description}")
    {
        Position = position;
    }

    public int Position { get; }
}
