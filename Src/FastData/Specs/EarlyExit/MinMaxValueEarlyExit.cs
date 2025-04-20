using Genbox.FastData.Abstracts;

namespace Genbox.FastData.Specs.EarlyExit;

public record MinMaxValueEarlyExit(object MinValue, object MaxValue) : IEarlyExit;