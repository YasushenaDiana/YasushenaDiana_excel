namespace Lab1.Core.Expressions;

public abstract class ExpressionException : Exception
{
    protected ExpressionException(string message) : base(message)
    {
    }
}
