namespace Genbox.FastData.Internal.Analysis.Properties;

internal sealed record NumericKeyProperties<T>(T MinKeyValue, T MaxKeyValue, ulong Range, double Density, bool HasZeroOrNaN, bool IsConsecutive, ulong BitMask, Func<T, long> ValueConverter) : IProperties;