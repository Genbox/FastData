using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Structures;

internal sealed class SingleValueStructure<TKey, TValue> : IStructure<TKey, TValue, SingleValueContext<TKey, TValue>>
{
    public SingleValueContext<TKey, TValue> Create(TKey[] data, ValueSpec<TValue>? valueSpec)
    {
        if (data.Length != 1)
            throw new InvalidOperationException("SingleValueStructure can only be created from a span with exactly one element.");

        return new SingleValueContext<TKey, TValue>(data[0], valueSpec);
    }
}