using Genbox.FastData.Abstracts;

namespace Genbox.FastData.Specs.EarlyExit;

public record MinMaxLengthEarlyExit(uint MinValue, uint MaxValue) : IEarlyExit;