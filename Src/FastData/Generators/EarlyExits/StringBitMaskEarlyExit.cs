using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits;

/// <summary>Uses a bitmask from the first bytes of the string to quickly reject non-matching keys.</summary>
public sealed record StringBitMaskEarlyExit(ulong Mask, int ByteCount) : IEarlyExit;