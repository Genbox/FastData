using Genbox.FastData.Abstracts;

namespace Genbox.FastData.EarlyExitSpecs;

public record MinMaxLengthEarlyExit(uint MinValue, uint MaxValue) : IEarlyExit;