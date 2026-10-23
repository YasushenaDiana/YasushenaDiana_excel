using Lab1.Core.Table;

namespace Lab1.Tests;

public class CellAddressTests
{
    [Theory]
    [InlineData(0, "A")]
    [InlineData(25, "Z")]
    [InlineData(26, "AA")]
    [InlineData(51, "AZ")]
    [InlineData(701, "ZZ")]
    [InlineData(702, "AAA")]
    public void GetColumnName(int column, string expected) =>
        Assert.Equal(expected, CellAddress.GetColumnName(column));

    [Theory]
    [InlineData("A1", 0, 0)]
    [InlineData("b2", 1, 1)]
    [InlineData("Z10", 9, 25)]
    [InlineData("AA3", 2, 26)]
    public void Parse_ValidAddress(string text, int row, int column)
    {
        var address = CellAddress.Parse(text);

        Assert.Equal(new CellAddress(row, column), address);
        Assert.Equal(text.ToUpperInvariant(), address.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("1")]
    [InlineData("1A")]
    [InlineData("A0")]
    [InlineData("A1B")]
    [InlineData("A99999999999")]
    public void TryParse_InvalidAddress(string text) =>
        Assert.False(CellAddress.TryParse(text, out _));
}
