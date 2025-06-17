namespace Genbox.FastData.Internal.Analysis.Properties;

internal sealed record ValueProperties<T>(T MinValue, T MaxValue, bool HasZeroOrNaN) : IProperties;