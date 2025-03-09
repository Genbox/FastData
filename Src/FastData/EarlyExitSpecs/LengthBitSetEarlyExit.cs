using Genbox.FastData.Abstracts;

namespace Genbox.FastData.EarlyExitSpecs;

public record LengthBitSetEarlyExit(ulong BitSet) : IEarlyExit;