using Genbox.FastData.Abstracts;

namespace Genbox.FastData.Specs.EarlyExit;

public record MinMaxUnsignedValueEarlyExit(ulong MinValue, ulong MaxValue) : IEarlyExit;