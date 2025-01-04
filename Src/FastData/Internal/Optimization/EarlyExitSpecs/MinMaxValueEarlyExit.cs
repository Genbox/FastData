using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Optimization.EarlyExitSpecs;

internal record MinMaxValueEarlyExit(long MinValue, long MaxValue) : IEarlyExit;