using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits;

/// <summary>Represents an early exit strategy that uses a bitset to quickly determine valid string lengths.</summary>
/// <param name="BitSet">A bit set where each bit represents a valid string length.</param>
public sealed record LengthBitSetEarlyExit(ulong[] BitSet) : IEarlyExit;