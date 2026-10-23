using System.Numerics;
using Lab1.Core.Expressions;
using Lab1.Core.Table;

namespace Lab1.Tests;

public class CalculatorTests
{
    private sealed class NoCells : ICellValueProvider
    {
        public BigInteger GetCellValue(CellAddress address) =>
            throw new ExpressionEvaluationException($"Клітинки {address} немає");
    }

    private readonly Calculator _calculator = new();

    private string Evaluate(string expression) =>
        _calculator.Evaluate(expression, new NoCells()).ToString();

    [Theory]
    [InlineData("1 + 2", "3")]
    [InlineData("2 + 3 * 4", "14")]
    [InlineData("(2 + 3) * 4", "20")]
    [InlineData("10 - 4 - 3", "3")]
    [InlineData("  8   *   ( 1 + 1 )  ", "16")]
    [InlineData("((7))", "7")]
    public void BinaryOperations_FollowPrecedence(string expression, string expected) =>
        Assert.Equal(expected, Evaluate(expression));

    [Theory]
    [InlineData("-5", "-5")]
    [InlineData("+7", "7")]
    [InlineData("--3", "3")]
    [InlineData("-(5 + 3)", "-8")]
    [InlineData("2 * -3", "-6")]
    [InlineData("-5 + 3", "-2")]
    public void UnaryOperations(string expression, string expected) =>
        Assert.Equal(expected, Evaluate(expression));

    [Theory]
    [InlineData("2 ^ 10", "1024")]
    [InlineData("2 ^ 3 ^ 2", "512")]
    [InlineData("-2 ^ 2", "-4")]
    [InlineData("(-2) ^ 2", "4")]
    [InlineData("(-2) ^ 3", "-8")]
    [InlineData("5 ^ 0", "1")]
    [InlineData("0 ^ 0", "1")]
    [InlineData("2 ^ 100", "1267650600228229401496703205376")]
    public void Power(string expression, string expected) =>
        Assert.Equal(expected, Evaluate(expression));

    [Theory]
    [InlineData("7 / 2", "3")]
    [InlineData("-7 / 2", "-3")]
    [InlineData("17 div 5", "3")]
    [InlineData("-7 div 2", "-4")]
    [InlineData("7 div -2", "-3")]
    [InlineData("17 mod 5", "2")]
    [InlineData("-7 mod 2", "1")]
    [InlineData("7 mod -2", "1")]
    [InlineData("17 MOD 5", "2")]
    [InlineData("mod(17, 5)", "2")]
    [InlineData("div(17, 5)", "3")]
    [InlineData("10 - 7 mod 4", "7")]
    public void Division(string expression, string expected) =>
        Assert.Equal(expected, Evaluate(expression));

    [Theory]
    [InlineData("-7", "2")]
    [InlineData("7", "2")]
    [InlineData("-7", "-2")]
    [InlineData("123456789", "-1000")]
    public void DivAndMod_SatisfyEuclideanDivision(string left, string right)
    {
        var a = BigInteger.Parse(left);
        var b = BigInteger.Parse(right);

        var div = LabCalculatorVisitor.Div(a, b);
        var mod = LabCalculatorVisitor.Mod(a, b);

        Assert.Equal(a, b * div + mod);
        Assert.True(mod >= 0 && mod < BigInteger.Abs(b));
    }

    [Theory]
    [InlineData("123456789012345678901234567890 * 10", "1234567890123456789012345678900")]
    [InlineData("99999999999999999999 + 1", "100000000000000000000")]
    [InlineData("100000000000000000000 - 1", "99999999999999999999")]
    public void BigIntegers(string expression, string expected) =>
        Assert.Equal(expected, Evaluate(expression));

    [Theory]
    [InlineData("1 +", 4)]
    [InlineData("(1 + 2", 7)]
    [InlineData("1 2", 3)]
    [InlineData("2 & 3", 3)]
    [InlineData("* 3", 1)]
    [InlineData("1 + ) 2", 5)]
    [InlineData("abc", 1)]
    [InlineData("", 1)]
    public void SyntaxErrors_HavePosition(string expression, int expectedPosition)
    {
        var exception = Assert.Throws<ExpressionSyntaxException>(() => _calculator.Parse(expression));
        Assert.Equal(expectedPosition, exception.Position);
    }

    [Theory]
    [InlineData("1 / 0")]
    [InlineData("5 mod 0")]
    [InlineData("5 div (2 - 2)")]
    [InlineData("2 ^ -1")]
    [InlineData("2 ^ 100000")]
    public void EvaluationErrors(string expression) =>
        Assert.Throws<ExpressionEvaluationException>(() => Evaluate(expression));

    [Fact]
    public void Parse_CollectsCellReferences()
    {
        var parsed = _calculator.Parse("A1 + b2 * A1 - AA10");

        Assert.Equal(
            new[] { new CellAddress(0, 0), new CellAddress(1, 1), new CellAddress(9, 26) }.ToHashSet(),
            parsed.References.ToHashSet());
    }
}
