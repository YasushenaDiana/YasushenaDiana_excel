using System.Diagnostics.CodeAnalysis;

namespace Lab1.Core.Table;

public readonly record struct CellAddress(int Row, int Column)
{
    public override string ToString() => GetColumnName(Column) + (Row + 1);

    public static string GetColumnName(int column)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(column);

        var name = string.Empty;
        var number = column + 1;
        while (number > 0)
        {
            var remainder = (number - 1) % 26;
            name = (char)('A' + remainder) + name;
            number = (number - 1) / 26;
        }
        return name;
    }

    public static bool TryParse(string text, [NotNullWhen(true)] out CellAddress? address)
    {
        address = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var value = text.Trim().ToUpperInvariant();
        var lettersCount = 0;
        while (lettersCount < value.Length && value[lettersCount] is >= 'A' and <= 'Z')
        {
            lettersCount++;
        }

        if (lettersCount == 0 || lettersCount == value.Length)
        {
            return false;
        }

        long column = 0;
        for (var i = 0; i < lettersCount; i++)
        {
            column = column * 26 + (value[i] - 'A' + 1);
            if (column > int.MaxValue)
            {
                return false;
            }
        }

        var digits = value[lettersCount..];
        if (!digits.All(char.IsAsciiDigit) || !int.TryParse(digits, out var row) || row < 1)
        {
            return false;
        }

        address = new CellAddress(row - 1, (int)column - 1);
        return true;
    }

    public static CellAddress Parse(string text) =>
        TryParse(text, out var address)
            ? address.Value
            : throw new FormatException($"Некоректна адреса клітинки: '{text}'");
}
