using Genbox.FastData.Internal.Analysis.Data;

namespace Genbox.FastData.Internal.Analysis.Properties;

internal sealed record NumericKeyProperties<T>(DataRanges<T> DataRanges, ulong Range, float Density, bool HasZero, bool IsConsecutive, ulong BitMask) : IProperties;