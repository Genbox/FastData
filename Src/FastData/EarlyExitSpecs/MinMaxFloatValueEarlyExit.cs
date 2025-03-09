using Genbox.FastData.Abstracts;

namespace Genbox.FastData.EarlyExitSpecs;

public record MinMaxFloatValueEarlyExit(double MinValue, double MaxValue) : IEarlyExit;