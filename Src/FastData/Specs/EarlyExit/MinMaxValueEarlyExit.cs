using Genbox.FastData.Abstracts;

namespace Genbox.FastData.Specs.EarlyExit;

public record MinMaxValueEarlyExit<T>(T MinValue, T MaxValue) : IEarlyExit;