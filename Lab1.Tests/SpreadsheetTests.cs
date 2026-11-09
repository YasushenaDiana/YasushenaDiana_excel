using System.Numerics;
using Lab1.Core.Table;

namespace Lab1.Tests;

public class SpreadsheetTests
{
    private readonly CellAddress A1 = new(0, 0);
    private readonly CellAddress B1 = new(0, 1);

    [Fact]
    public void Expression_IsCalculatedWithOperatorPriority()
    {
        var sheet = new Spreadsheet();

        sheet.SetExpression(A1, "2 + 3 * 4");

        Assert.Equal(new BigInteger(14), sheet.GetCell(A1).Value);
    }

    [Fact]
    public void ModAndDiv_AreCalculatedCorrectly()
    {
        var sheet = new Spreadsheet();

        sheet.SetExpression(A1, "17 mod 5");
        sheet.SetExpression(B1, "17 div 5");

        Assert.Equal(new BigInteger(2), sheet.GetCell(A1).Value);
        Assert.Equal(new BigInteger(3), sheet.GetCell(B1).Value);
    }

    [Fact]
    public void CellReference_UsesValueOfAnotherCell()
    {
        var sheet = new Spreadsheet();

        sheet.SetExpression(A1, "10");
        sheet.SetExpression(B1, "A1 + 5");

        Assert.Equal(new BigInteger(15), sheet.GetCell(B1).Value);
    }

    [Fact]
    public void SyntaxError_IsShownWithPosition()
    {
        var sheet = new Spreadsheet();

        sheet.SetExpression(A1, "1 + * 3");

        Assert.True(sheet.GetCell(A1).HasError);
        Assert.Contains("позиція 5", sheet.GetCell(A1).Error);
    }
}
