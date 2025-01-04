using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Optimization.EarlyExitSpecs;

internal record MinMaxFloatValueEarlyExit(double MinValue, double MaxValue) : IEarlyExit;