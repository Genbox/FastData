using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits;

/// <summary>Represents an early exit strategy that checks if a value matches a specific length.</summary>
/// <param name="Length">The string length.</param>
/// <param name="ByteCount">The byte count.</param>
public sealed record LengthEqualEarlyExit(uint Length, uint ByteCount) : IEarlyExit;