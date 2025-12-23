namespace Genbox.FastData.Internal.Analysis.Properties;

internal sealed record NumericKeyProperties<T>(T MinKeyValue, T MaxKeyValue, ulong Range, bool HasZeroOrNaN, bool IsConsecutive) : IProperties;