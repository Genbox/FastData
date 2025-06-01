using Genbox.FastData.Abstracts;

namespace Genbox.FastData.EarlyExits;

public sealed record LengthBitSetEarlyExit(ulong BitSet) : IEarlyExit;