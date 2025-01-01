using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Optimization.EarlyExitSpecs;

internal record MinMaxLengthEarlyExitSpec(uint MinStrLength, uint MaxStrLength) : IEarlyExitSpec;