using Lab1.Core.Grammar;
using Lab1.Core.Table;

namespace Lab1.Core.Expressions;

public sealed class ParsedExpression
{
    internal ParsedExpression(string text, LabCalculatorParser.CompileUnitContext tree,
        IReadOnlySet<CellAddress> references)
    {
        Text = text;
        Tree = tree;
        References = references;
    }

    public string Text { get; }

    public IReadOnlySet<CellAddress> References { get; }

    internal LabCalculatorParser.CompileUnitContext Tree { get; }
}
