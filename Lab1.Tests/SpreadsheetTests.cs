using System.Numerics;
using Lab1.Core.Table;

namespace Lab1.Tests;

public class SpreadsheetTests
{
    private static readonly CellAddress A1 = CellAddress.Parse("A1");
    private static readonly CellAddress A2 = CellAddress.Parse("A2");
    private static readonly CellAddress B1 = CellAddress.Parse("B1");
    private static readonly CellAddress C1 = CellAddress.Parse("C1");

    [Fact]
    public void NewSpreadsheet_HasDefaultSizeAndEmptyCells()
    {
        var sheet = new Spreadsheet();

        Assert.Equal(Spreadsheet.DefaultRowCount, sheet.RowCount);
        Assert.Equal(Spreadsheet.DefaultColumnCount, sheet.ColumnCount);
        Assert.All(sheet.Cells, cell => Assert.True(cell.IsEmpty));
        Assert.False(sheet.IsModified);
    }

    [Fact]
    public void References_AreEvaluatedAndUpdated()
    {
        var sheet = new Spreadsheet(3, 3);
        sheet.SetExpression(A1, "5");
        sheet.SetExpression(B1, "A1 * 2");

        Assert.Equal(new BigInteger(10), sheet.GetCell(B1).Value);

        sheet.SetExpression(A1, "7");

        Assert.Equal(new BigInteger(14), sheet.GetCell(B1).Value);
        Assert.True(sheet.IsModified);
    }

    [Fact]
    public void EmptyCell_IsZero()
    {
        var sheet = new Spreadsheet(3, 3);
        sheet.SetExpression(B1, "A1 + 3");

        Assert.Equal(new BigInteger(3), sheet.GetCell(B1).Value);
    }

    [Fact]
    public void LowercaseReferences_Work()
    {
        var sheet = new Spreadsheet(3, 3);
        sheet.SetExpression(A1, "4");
        sheet.SetExpression(B1, "a1 ^ 2");

        Assert.Equal(new BigInteger(16), sheet.GetCell(B1).Value);
    }

    [Fact]
    public void CircularReference_IsError()
    {
        var sheet = new Spreadsheet(3, 3);
        sheet.SetExpression(A1, "B1 + 1");
        sheet.SetExpression(B1, "A1 + 1");

        Assert.Contains("Циклічне", sheet.GetCell(A1).Error);
        Assert.Contains("Циклічне", sheet.GetCell(B1).Error);
    }

    [Fact]
    public void SelfReference_IsError()
    {
        var sheet = new Spreadsheet(3, 3);
        sheet.SetExpression(A1, "A1 + 1");

        Assert.True(sheet.GetCell(A1).HasError);
    }

    [Fact]
    public void BreakingCycle_RemovesErrors()
    {
        var sheet = new Spreadsheet(3, 3);
        sheet.SetExpression(A1, "B1 + 1");
        sheet.SetExpression(B1, "A1 + 1");

        sheet.SetExpression(B1, "10");

        Assert.False(sheet.GetCell(A1).HasError);
        Assert.Equal(new BigInteger(11), sheet.GetCell(A1).Value);
    }

    [Fact]
    public void ErrorInReferencedCell_Propagates()
    {
        var sheet = new Spreadsheet(3, 3);
        sheet.SetExpression(A1, "1 / 0");
        sheet.SetExpression(B1, "A1 + 1");

        Assert.Equal("Ділення на нуль", sheet.GetCell(A1).Error);
        Assert.Contains("A1", sheet.GetCell(B1).Error);
    }

    [Fact]
    public void ReferenceOutsideTable_IsError()
    {
        var sheet = new Spreadsheet(3, 3);
        sheet.SetExpression(A1, "Z100 + 1");

        Assert.Contains("немає в таблиці", sheet.GetCell(A1).Error);
    }

    [Fact]
    public void SyntaxError_IsStoredInCell()
    {
        var sheet = new Spreadsheet(3, 3);
        sheet.SetExpression(A1, "2 + * 3");

        Assert.Contains("позиція 5", sheet.GetCell(A1).Error);
        Assert.Equal("#ПОМИЛКА", sheet.GetCell(A1).GetDisplayText(showValues: true));
        Assert.Equal("2 + * 3", sheet.GetCell(A1).GetDisplayText(showValues: false));
    }

    [Fact]
    public void AddRowAndColumn_ChangeSize()
    {
        var sheet = new Spreadsheet(2, 2);

        sheet.AddRow();
        sheet.AddColumn();

        Assert.Equal(3, sheet.RowCount);
        Assert.Equal(3, sheet.ColumnCount);
        Assert.True(sheet.GetCell(new CellAddress(2, 2)).IsEmpty);
    }

    [Fact]
    public void RemoveLastColumn_MakesReferencesInvalid()
    {
        var sheet = new Spreadsheet(2, 3);
        sheet.SetExpression(A1, "C1 + 1");
        sheet.SetExpression(C1, "5");

        Assert.True(sheet.IsLastColumnInUse());

        sheet.RemoveLastColumn();

        Assert.Equal(2, sheet.ColumnCount);
        Assert.True(sheet.GetCell(A1).HasError);
    }

    [Fact]
    public void IsLastRowInUse_DetectsDataAndReferences()
    {
        var sheet = new Spreadsheet(2, 2);
        Assert.False(sheet.IsLastRowInUse());

        sheet.SetExpression(A1, "A2 * 2");
        Assert.True(sheet.IsLastRowInUse());

        sheet.SetExpression(A1, "1");
        sheet.SetExpression(A2, "3");
        Assert.True(sheet.IsLastRowInUse());
    }

    [Fact]
    public void CannotRemoveLastRemainingRowOrColumn()
    {
        var sheet = new Spreadsheet(1, 1);

        Assert.Throws<InvalidOperationException>(sheet.RemoveLastRow);
        Assert.Throws<InvalidOperationException>(sheet.RemoveLastColumn);
    }
}
