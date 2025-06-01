using Genbox.FastData.Abstracts;

namespace Genbox.FastData.EarlyExits;

public sealed record MinMaxValueEarlyExit<T>(T MinValue, T MaxValue) : IEarlyExit;