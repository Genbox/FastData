using System.Text;
using static Genbox.FastData.Internal.Compat.BitOperations;

namespace Genbox.FastData.Internal.Analysis.Genetic;

internal static class Extractors
{
    internal static readonly Func<uint, ushort, StringBuilder?, uint>[] Ops =
    [
        // FNV1aRound,
        // DJB2Round,
        // XXHashRound,
        // XXHashRound2,

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
            z?.Append("Mult->");
            return x * y;
        },
    ];

    private static uint FNV1aRound(uint hash, ushort input, StringBuilder? sb)
    {
        sb?.Append("FNV1aRound->");
        return (hash ^ input) * 16777619;
    }

    private static uint DJB2Round(uint hash, ushort input, StringBuilder? sb)
    {
        sb?.Append("DJB2Round->");
        return ((hash << 5) + hash) ^ input;
    }

    private static uint XXHashRound(uint hash, ushort input, StringBuilder? sb)
    {
        sb?.Append("XXHashRound->");
        hash += input * 2246822519U;
        hash = RotateLeft(hash, 13);
        hash *= 2654435761U;
        return hash;
    }

    private static uint XXHashRound2(uint hash, ushort input, StringBuilder? sb)
    {
        sb?.Append("XXHashRound2->");
        hash += input * 2246822519U;
        hash = RotateLeft(hash, 13);
        hash *= 2654435761U;
        hash = RotateLeft(hash, 15);
        hash *= 2654435763U;
        return hash;
    }
}