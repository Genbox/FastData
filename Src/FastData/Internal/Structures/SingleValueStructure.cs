using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Structures;

internal sealed class SingleValueStructure<TKey, TValue> : IStructure<TKey, TValue, SingleValueContext<TKey, TValue>>
{
    public SingleValueContext<TKey, TValue> Create(TKey[] keys, TValue[]? values)
    {
        if (keys.Length != 1)
            throw new InvalidOperationException("SingleValueStructure can only be created from a span with exactly one element.");

        return new SingleValueContext<TKey, TValue>(keys[0], values);
    }
}