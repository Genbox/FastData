using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Optimization.EarlyExitSpecs;

internal record LengthBitSetEarlyExit(ulong BitSet) : IEarlyExit;