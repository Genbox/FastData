using Genbox.FastData.Abstracts;

namespace Genbox.FastData.Specs.EarlyExit;

public record MinMaxValueEarlyExit(long MinValue, long MaxValue) : IEarlyExit;