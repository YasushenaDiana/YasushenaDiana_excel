using System.Numerics;
using Lab1.Core.Grammar;
using Lab1.Core.Table;

namespace Lab1.Core.Expressions;

public sealed class LabCalculatorVisitor : LabCalculatorBaseVisitor<BigInteger>
{
    public const int MaxExponent = 10_000;
    public const long MaxResultBits = 1_000_000;

    private readonly ICellValueProvider _cells;

    public LabCalculatorVisitor(ICellValueProvider cells)
    {
        _cells = cells;
    }

    public override BigInteger VisitCompileUnit(LabCalculatorParser.CompileUnitContext context) =>
        Visit(context.expression());

    public override BigInteger VisitNumberExpr(LabCalculatorParser.NumberExprContext context) =>
        BigInteger.Parse(context.GetText());

    public override BigInteger VisitCellExpr(LabCalculatorParser.CellExprContext context)
    {
        var name = context.GetText().ToUpperInvariant();
        if (!CellAddress.TryParse(name, out var address))
        {
            throw new ExpressionEvaluationException($"Клітинки {name} не існує");
        }
        return _cells.GetCellValue(address.Value);
    }

    public override BigInteger VisitParenthesizedExpr(LabCalculatorParser.ParenthesizedExprContext context) =>
        Visit(context.expression());

    public override BigInteger VisitUnaryExpr(LabCalculatorParser.UnaryExprContext context)
    {
        var operand = Visit(context.expression());
        return context.operatorToken.Type == LabCalculatorLexer.SUBTRACT ? -operand : operand;
    }

    public override BigInteger VisitExponentialExpr(LabCalculatorParser.ExponentialExprContext context)
    {
        var left = Visit(context.expression(0));
        var right = Visit(context.expression(1));
        return Power(left, right);
    }

    public override BigInteger VisitMultiplicativeExpr(LabCalculatorParser.MultiplicativeExprContext context)
    {
        var left = Visit(context.expression(0));
        var right = Visit(context.expression(1));

        return context.operatorToken.Type switch
        {
            LabCalculatorLexer.MULTIPLY => left * right,
            LabCalculatorLexer.DIVIDE => Divide(left, right),
            LabCalculatorLexer.MOD => Mod(left, right),
            LabCalculatorLexer.DIV => Div(left, right),
            _ => throw new ExpressionEvaluationException($"Невідома операція '{context.operatorToken.Text}'")
        };
    }

    public override BigInteger VisitAdditiveExpr(LabCalculatorParser.AdditiveExprContext context)
    {
        var left = Visit(context.expression(0));
        var right = Visit(context.expression(1));
        return context.operatorToken.Type == LabCalculatorLexer.ADD ? left + right : left - right;
    }

    public override BigInteger VisitModDivFunctionExpr(LabCalculatorParser.ModDivFunctionExprContext context)
    {
        var left = Visit(context.expression(0));
        var right = Visit(context.expression(1));
        return context.operatorToken.Type == LabCalculatorLexer.MOD ? Mod(left, right) : Div(left, right);
    }

    public static BigInteger Divide(BigInteger left, BigInteger right)
    {
        EnsureNotZero(right);
        return BigInteger.Divide(left, right);
    }

    public static BigInteger Div(BigInteger left, BigInteger right)
    {
        EnsureNotZero(right);
        var quotient = BigInteger.DivRem(left, right, out var remainder);
        if (remainder.Sign < 0)
        {
            quotient += right.Sign > 0 ? -1 : 1;
        }
        return quotient;
    }

    public static BigInteger Mod(BigInteger left, BigInteger right)
    {
        EnsureNotZero(right);
        var remainder = BigInteger.Remainder(left, right);
        if (remainder.Sign < 0)
        {
            remainder += BigInteger.Abs(right);
        }
        return remainder;
    }

    public static BigInteger Power(BigInteger value, BigInteger exponent)
    {
        if (exponent.Sign < 0)
        {
            throw new ExpressionEvaluationException("Від'ємний степінь не підтримується: результат не є цілим числом");
        }
        if (value.IsZero)
        {
            return exponent.IsZero ? BigInteger.One : BigInteger.Zero;
        }
        if (value.IsOne)
        {
            return BigInteger.One;
        }
        if (value == BigInteger.MinusOne)
        {
            return exponent.IsEven ? BigInteger.One : BigInteger.MinusOne;
        }
        if (exponent > MaxExponent)
        {
            throw new ExpressionEvaluationException($"Занадто великий степінь (максимум {MaxExponent})");
        }
        if (value.GetBitLength() * (long)exponent > MaxResultBits)
        {
            throw new ExpressionEvaluationException("Результат піднесення до степеня занадто великий");
        }
        return BigInteger.Pow(value, (int)exponent);
    }

    private static void EnsureNotZero(BigInteger divisor)
    {
        if (divisor.IsZero)
        {
            throw new ExpressionEvaluationException("Ділення на нуль");
        }
    }
}
