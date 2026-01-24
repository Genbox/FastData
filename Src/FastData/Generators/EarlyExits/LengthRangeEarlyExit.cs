using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits;

/// <summary>Represents an early exit strategy that checks if a value is within a specified length range.</summary>
/// <param name="MinLength">The minimum string length.</param>
/// <param name="MaxLength">The maximum string length.</param>
/// <param name="MinLength">The minimum byte count.</param>
/// <param name="MaxLength">The maximum byte count.</param>
public sealed record LengthRangeEarlyExit(uint MinLength, uint MaxLength, uint MinByteCount, uint MaxByteCount) : IEarlyExit;