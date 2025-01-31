using System.Text;
using static Genbox.FastData.Internal.Compat.BitOperations;

namespace Genbox.FastData.Internal.Analysis.Genetic;

internal static class Extractors
{
    internal static readonly Func<uint, ushort, StringBuilder?, uint>[] Ops =
    [
        static (x, y, z) =>
        {
            z?.Append("Add->");
            return x + y;
        },
        static (x, y, z) =>
        {
            z?.Append("Xor->");
            return x ^ y;
        },
        static (x, y, z) =>
        {
            z?.Append("Mul->");
            return x * y;
        },
    ];
}