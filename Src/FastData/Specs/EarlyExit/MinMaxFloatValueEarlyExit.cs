using Genbox.FastData.Abstracts;

namespace Genbox.FastData.Specs.EarlyExit;

public record MinMaxFloatValueEarlyExit(double MinValue, double MaxValue) : IEarlyExit;