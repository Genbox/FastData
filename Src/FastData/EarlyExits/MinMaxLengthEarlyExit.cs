using Genbox.FastData.Abstracts;

namespace Genbox.FastData.EarlyExits;

public sealed record MinMaxLengthEarlyExit(uint MinValue, uint MaxValue) : IEarlyExit;