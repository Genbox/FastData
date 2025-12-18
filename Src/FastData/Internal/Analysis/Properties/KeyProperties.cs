namespace Genbox.FastData.Internal.Analysis.Properties;

internal sealed record KeyProperties<T>(T MinKeyValue, T MaxKeyValue, bool HasZeroOrNaN, bool IsContiguous) : IProperties;