namespace Genbox.FastData.Internal.Encodings;

/// <summary>
/// Signed integer transform used by Protocol Buffers sint32/sint64 before base-128 varint encoding.
/// Reference: Protocol Buffers encoding guide at https://protobuf.dev/programming-guides/encoding/#signed-ints.
/// </summary>
internal static class ZigZag
{
    internal static ulong Encode(long value) => unchecked(((ulong)value << 1) ^ (ulong)(value >> 63));

    internal static long Decode(ulong value) => unchecked((long)(value >> 1) ^ -((long)value & 1));
}