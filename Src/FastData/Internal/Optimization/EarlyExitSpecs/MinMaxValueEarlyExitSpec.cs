using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Optimization.EarlyExitSpecs;

internal record MinMaxValueEarlyExitSpec(long MinValue, long MaxValue) : IEarlyExitSpec;