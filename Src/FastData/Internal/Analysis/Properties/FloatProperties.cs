using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Analysis.Properties;

internal sealed record FloatProperties<T>(T MinValue, T MaxValue) : IHasMinMax<T>;