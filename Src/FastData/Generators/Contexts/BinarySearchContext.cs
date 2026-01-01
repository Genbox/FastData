namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides a context for binary search-based data structures.</summary>
public sealed class BinarySearchContext<TKey, TValue>(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values, bool useInterpolation) : DefaultContext<TKey, TValue>(keys, values)
{
    public bool UseInterpolation { get; } = useInterpolation;
}