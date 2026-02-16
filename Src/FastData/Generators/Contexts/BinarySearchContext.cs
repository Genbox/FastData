using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

public sealed class BinarySearchContext<TKey, TValue>(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values, bool useInterpolation) : BinarySearchContext(useInterpolation)
{
    public ReadOnlyMemory<TKey> Keys { get; } = keys;
    public ReadOnlyMemory<TValue> Values { get; } = values;
}

public abstract class BinarySearchContext(bool useInterpolation) : IContext
{
    public bool UseInterpolation { get; } = useInterpolation;
}