using Genbox.FastData.Abstracts;

namespace Genbox.FastData.EarlyExits;

public record MinMaxLengthEarlyExit(uint MinValue, uint MaxValue) : IEarlyExit;