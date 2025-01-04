using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Optimization.EarlyExitSpecs;

internal record MinMaxLengthEarlyExit(uint MinLength, uint MaxLength) : IEarlyExit;