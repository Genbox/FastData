using System.Text;
using Genbox.FastData.Internal.Compat;

namespace Genbox.FastData.Internal.Analysis.Genetic;

internal static class Mixers
{
    internal static readonly Func<uint, StringBuilder?, uint>[] Ops =
    [
        (x, y) =>
        {
            y?.Append("Identity->");
            return x;
        },
        (x, y) =>
        {
            y?.Append("AddMix->");
            return x + Seeds.GoodSeeds[0];
        },
        (x, y) =>
        {
            y?.Append("MulMix->");
            return x * Seeds.GoodSeeds[0];
        },
        (x, y) =>
        {
            y?.Append("SquareMix->");
            return (1u | x) + (x * x);
        },
        (x, y) =>
        {
            y?.Append("XorShiftMix->");
            return x ^ (x >> 16);
        },
        (x, y) =>
        {
            y?.Append("ShiftAddXorMix->");
            return ((x << 5) + x) ^ x;
        },
        (x, y) =>
        {
            y?.Append("RotateRightMix->");
            return BitOperations.RotateRight(x, 16);
        },
        (x, y) =>
        {
            y?.Append("RotateLeftMix->");
            return BitOperations.RotateLeft(x, 16);
        }
    ];
}