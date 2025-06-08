using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits;

public sealed record MinMaxLengthEarlyExit(uint MinValue, uint MaxValue) : IEarlyExit;