using Genbox.FastData.Abstracts;

namespace Genbox.FastData.Specs.EarlyExit;

public record LengthBitSetEarlyExit(ulong BitSet) : IEarlyExit;