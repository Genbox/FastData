using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits;

/// <summary>Represents an early exit strategy that checks for bits that can never be set.</summary>
/// <param name="Mask">A mask of bits that are never set in any key.</param>
public sealed record ValueBitMaskEarlyExit(ulong Mask) : IEarlyExit;