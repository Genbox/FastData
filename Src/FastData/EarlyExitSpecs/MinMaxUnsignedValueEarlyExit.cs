using Genbox.FastData.Abstracts;

namespace Genbox.FastData.EarlyExitSpecs;

public record MinMaxUnsignedValueEarlyExit(ulong MinValue, ulong MaxValue) : IEarlyExit;