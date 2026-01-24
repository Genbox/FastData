using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits;

/// <summary>Represents an early exit strategy that checks for a common length divisor.</summary>
/// <param name="CharDivisor">The divisor that all character lengths must be divisible by.</param>
/// <param name="ByteDivisor">The divisor that all byte lengths must be divisible by.</param>
public sealed record LengthDivisorEarlyExit(uint CharDivisor, uint ByteDivisor) : IEarlyExit;
