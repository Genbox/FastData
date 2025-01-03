using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Optimization.EarlyExitSpecs;

internal record MinMaxLengthEarlyExitSpec(uint MinLength, uint MaxLength) : IEarlyExitSpec;