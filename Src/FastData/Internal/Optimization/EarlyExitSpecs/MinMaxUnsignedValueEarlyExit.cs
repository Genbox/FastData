using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Optimization.EarlyExitSpecs;

internal record MinMaxUnsignedValueEarlyExit(ulong MinValue, ulong MaxValue) : IEarlyExit;