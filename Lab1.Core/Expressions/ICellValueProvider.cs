using System.Numerics;
using Lab1.Core.Table;

namespace Lab1.Core.Expressions;

public interface ICellValueProvider
{
    BigInteger GetCellValue(CellAddress address);
}
