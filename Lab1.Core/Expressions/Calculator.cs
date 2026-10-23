using System.Numerics;
using Antlr4.Runtime;
using Lab1.Core.Grammar;
using Lab1.Core.Table;

namespace Lab1.Core.Expressions;

public sealed class Calculator
{
    public ParsedExpression Parse(string expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var errorListener = new ThrowExceptionErrorListener(expression);

        var lexer = new LabCalculatorLexer(new AntlrInputStream(expression));
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(errorListener);

        var tokens = new CommonTokenStream(lexer);
        var parser = new LabCalculatorParser(tokens);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(errorListener);

        var tree = parser.compileUnit();

        var references = tokens.GetTokens()
            .Where(token => token.Type == LabCalculatorLexer.CELL)
            .Select(token => CellAddress.TryParse(token.Text, out var address) ? address : null)
            .OfType<CellAddress>()
            .ToHashSet();

        return new ParsedExpression(expression, tree, references);
    }

    public BigInteger Evaluate(ParsedExpression expression, ICellValueProvider cells) =>
        new LabCalculatorVisitor(cells).Visit(expression.Tree);

    public BigInteger Evaluate(string expression, ICellValueProvider cells) =>
        Evaluate(Parse(expression), cells);
}
