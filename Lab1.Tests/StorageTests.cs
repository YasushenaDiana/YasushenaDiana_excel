using System.Numerics;
using System.Text;
using Lab1.Core.Storage;
using Lab1.Core.Table;

namespace Lab1.Tests;

public class StorageTests
{
    private readonly JsonSpreadsheetStorage _storage = new();

    private Spreadsheet LoadTestFile(string fileName)
    {
        using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "TestFiles", fileName));
        return _storage.Load(stream);
    }

    private static string? ValueOf(Spreadsheet sheet, string address) =>
        sheet.GetCell(CellAddress.Parse(address)).Value?.ToString();

    [Fact]
    public void SaveAndLoad_KeepsSizeExpressionsAndValues()
    {
        var sheet = new Spreadsheet(4, 3);
        sheet.SetExpression(CellAddress.Parse("A1"), "2 ^ 64");
        sheet.SetExpression(CellAddress.Parse("C4"), "A1 mod 1000");

        using var stream = new MemoryStream();
        _storage.Save(sheet, stream);
        stream.Position = 0;
        var loaded = _storage.Load(stream);

        Assert.Equal(4, loaded.RowCount);
        Assert.Equal(3, loaded.ColumnCount);
        Assert.Equal("A1 mod 1000", loaded.GetCell(CellAddress.Parse("C4")).Expression);
        Assert.Equal(BigInteger.Pow(2, 64) % 1000, loaded.GetCell(CellAddress.Parse("C4")).Value);
        Assert.False(loaded.IsModified);
    }

    [Fact]
    public void ArithmeticTestFile()
    {
        var sheet = LoadTestFile("01_arithmetic.json");

        Assert.Equal("3", ValueOf(sheet, "A1"));
        Assert.Equal("14", ValueOf(sheet, "A2"));
        Assert.Equal("20", ValueOf(sheet, "A3"));
        Assert.Equal("3", ValueOf(sheet, "A4"));
        Assert.Equal("512", ValueOf(sheet, "A5"));
        Assert.Equal("-4", ValueOf(sheet, "A6"));
        Assert.Equal("4", ValueOf(sheet, "A7"));
        Assert.Equal("-8", ValueOf(sheet, "A8"));
        Assert.Equal("7", ValueOf(sheet, "A9"));
        Assert.Equal("1267650600228229401496703205376", ValueOf(sheet, "A10"));
        Assert.Equal("3", ValueOf(sheet, "B1"));
        Assert.Equal("-3", ValueOf(sheet, "B2"));
        Assert.Equal("3", ValueOf(sheet, "B3"));
        Assert.Equal("-4", ValueOf(sheet, "B4"));
        Assert.Equal("2", ValueOf(sheet, "B5"));
        Assert.Equal("1", ValueOf(sheet, "B6"));
        Assert.Equal("2", ValueOf(sheet, "B7"));
        Assert.Equal("3", ValueOf(sheet, "B8"));
        Assert.Equal("1234567890123456789012345678900", ValueOf(sheet, "B9"));
        Assert.Equal("100000000000000000000", ValueOf(sheet, "B10"));
        Assert.DoesNotContain(sheet.Cells, cell => cell.HasError);
    }

    [Fact]
    public void ReferencesTestFile()
    {
        var sheet = LoadTestFile("02_references.json");

        Assert.Equal("125", ValueOf(sheet, "A3"));
        Assert.Equal("150", ValueOf(sheet, "A4"));
        Assert.Equal("2", ValueOf(sheet, "B1"));
        Assert.Equal("14", ValueOf(sheet, "B2"));
        Assert.Equal("100", ValueOf(sheet, "B3"));
        Assert.Equal("5625", ValueOf(sheet, "B4"));
        Assert.Equal("1", ValueOf(sheet, "C1"));
        Assert.Equal("102", ValueOf(sheet, "C2"));
    }

    [Fact]
    public void ErrorsTestFile()
    {
        var sheet = LoadTestFile("03_errors.json");

        foreach (var address in new[] { "A1", "A2", "A3", "A4", "A5", "B1", "C1", "B2", "B3", "B4" })
        {
            Assert.True(sheet.GetCell(CellAddress.Parse(address)).HasError, $"{address} має містити помилку");
        }
        Assert.Contains("Синтаксична", sheet.GetCell(CellAddress.Parse("A3")).Error);
        Assert.Contains("Циклічне", sheet.GetCell(CellAddress.Parse("B1")).Error);
        Assert.Equal("10", ValueOf(sheet, "C5"));
    }

    [Fact]
    public void DocumentWithoutCells_LoadsEmptyTable()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("""{ "rows": 2, "columns": 2, "cells": null }"""));

        var sheet = _storage.Load(stream);

        Assert.Equal(2, sheet.RowCount);
        Assert.All(sheet.Cells, cell => Assert.True(cell.IsEmpty));
    }

    [Fact]
    public void BrokenTestFile_ThrowsInvalidData() =>
        Assert.Throws<InvalidDataException>(() => LoadTestFile("04_broken_file.json"));

    [Theory]
    [InlineData("""{ "rows": 0, "columns": 3, "cells": [] }""")]
    [InlineData("""{ "rows": 3, "columns": 3, "cells": [ { "address": "Z99", "expression": "1" } ] }""")]
    [InlineData("""{ "rows": 3, "columns": 3, "cells": [ { "address": "???", "expression": "1" } ] }""")]
    [InlineData("""{ "rows": 3, "columns": 500, "cells": [] }""")]
    [InlineData("null")]
    public void InvalidDocument_ThrowsInvalidData(string json)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        Assert.Throws<InvalidDataException>(() => _storage.Load(stream));
    }
}
