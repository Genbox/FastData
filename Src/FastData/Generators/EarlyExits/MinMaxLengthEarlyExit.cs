using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits;

/// <summary>Represents an early exit strategy that checks if a value is within a specified minimum and maximum length.</summary>
/// <param name="MinLength">The minimum length.</param>
/// <param name="MaxLength">The maximum length.</param>
public sealed record MinMaxLengthEarlyExit(uint MinLength, uint MaxLength) : IEarlyExit;