using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits;

public sealed record LengthBitSetEarlyExit(ulong BitSet) : IEarlyExit;