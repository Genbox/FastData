using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Analysis.Properties;

internal sealed record IntegerProperties<T>(T MinValue, T MaxValue) : IHasMinMax<T>;