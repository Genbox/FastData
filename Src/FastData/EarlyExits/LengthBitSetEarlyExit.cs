using Genbox.FastData.Abstracts;

namespace Genbox.FastData.EarlyExits;

public record LengthBitSetEarlyExit(ulong BitSet) : IEarlyExit;