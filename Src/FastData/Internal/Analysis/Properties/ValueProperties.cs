namespace Genbox.FastData.Internal.Analysis.Properties;

internal sealed record ValueProperties<T>(T MinKeyValue, T MaxKeyValue, bool HasZeroOrNaN) : IProperties;