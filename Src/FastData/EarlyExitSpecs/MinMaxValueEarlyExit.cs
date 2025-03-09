using Genbox.FastData.Abstracts;

namespace Genbox.FastData.EarlyExitSpecs;

public record MinMaxValueEarlyExit(long MinValue, long MaxValue) : IEarlyExit;