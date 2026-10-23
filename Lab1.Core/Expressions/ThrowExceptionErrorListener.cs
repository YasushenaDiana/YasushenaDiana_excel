using Antlr4.Runtime;

namespace Lab1.Core.Expressions;

internal sealed class ThrowExceptionErrorListener : BaseErrorListener, IAntlrErrorListener<int>
{
    private readonly string _expression;

    public ThrowExceptionErrorListener(string expression)
    {
        _expression = expression;
    }

    public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol,
        int line, int charPositionInLine, string msg, RecognitionException e)
    {
        var description = offendingSymbol.Type == TokenConstants.EOF
            ? "вираз обірвано, очікувалося продовження (наприклад, число, клітинка або ')')"
            : $"неочікуваний елемент '{offendingSymbol.Text}'";
        throw new ExpressionSyntaxException(charPositionInLine + 1, description);
    }

    public void SyntaxError(TextWriter output, IRecognizer recognizer, int offendingSymbol,
        int line, int charPositionInLine, string msg, RecognitionException e)
    {
        var symbol = charPositionInLine < _expression.Length
            ? _expression[charPositionInLine].ToString()
            : msg;
        throw new ExpressionSyntaxException(charPositionInLine + 1, $"недопустимий символ '{symbol}'");
    }
}
