using Lab1.Core.Table;

namespace Lab1.Core.Storage;

public interface ISpreadsheetStorage
{
    void Save(Spreadsheet sheet, Stream stream);

    Spreadsheet Load(Stream stream);
}
