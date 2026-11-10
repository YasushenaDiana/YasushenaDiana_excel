using System.Numerics;
using Lab1.Core.Grammar;
using Lab1.Core.Table;

namespace Lab1.Core.Expressions;

public sealed class LabCalculatorVisitor : LabCalculatorBaseVisitor<BigInteger>
{
    public const int MaxDigits = 1000;

    private static readonly BigInteger MaxValue = BigInteger.Pow(10, MaxDigits);

    private readonly ICellValueProvider _cells;

    public LabCalculatorVisitor(ICellValueProvider cells)
    {
        _cells = cells;
    }

    public override BigInteger VisitCompileUnit(LabCalculatorParser.CompileUnitContext context) =>
        Visit(context.expression());

    public override BigInteger VisitNumberExpr(LabCalculatorParser.NumberExprContext context) =>
        CheckSize(BigInteger.Parse(context.GetText()));

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
            LabCalculatorLexer.MULTIPLY => CheckSize(left * right),
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
        return CheckSize(context.operatorToken.Type == LabCalculatorLexer.ADD ? left + right : left - right);
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
        if (value.IsZero)
        {
            if (exponent.Sign < 0)
            {
                throw new ExpressionEvaluationException("Ділення на нуль: 0 у від'ємному степені");
            }
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
        if (exponent.Sign < 0)
        {
            throw new ExpressionEvaluationException("Від'ємний степінь дає дробове число, а програма працює лише з цілими");
        }
        if ((BigInteger.Abs(value).GetBitLength() - 1) * exponent > MaxDigits * 10 / 3)
        {
            throw TooLarge();
        }
        return CheckSize(BigInteger.Pow(value, (int)exponent));
    }

    private static BigInteger CheckSize(BigInteger value) =>
        BigInteger.Abs(value) < MaxValue ? value : throw TooLarge();

    private static ExpressionEvaluationException TooLarge() =>
        new($"Число занадто велике (більше {MaxDigits} цифр)");

    private static void EnsureNotZero(BigInteger divisor)
    {
        if (divisor.IsZero)
        {
            throw new ExpressionEvaluationException("Ділення на нуль");
        }
    }
}
