using Genbox.FastData.Abstracts;

namespace Genbox.FastData.EarlyExits;

public record MinMaxValueEarlyExit<T>(T MinValue, T MaxValue) : IEarlyExit;