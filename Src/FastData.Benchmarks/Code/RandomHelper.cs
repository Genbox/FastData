namespace Genbox.FastData.Benchmarks.Code;

using static System.Buffers.Binary.BinaryPrimitives;

internal static class RandomHelper
{
    internal static uint NextUInt32(Random rng)
    {
        Span<byte> bytes = stackalloc byte[4];
        rng.NextBytes(bytes);
        return ReadUInt32LittleEndian(bytes);
    }

    internal static ulong NextUInt64(Random rng)
    {
        Span<byte> bytes = stackalloc byte[8];
        rng.NextBytes(bytes);
        return ReadUInt64LittleEndian(bytes);
    }
}