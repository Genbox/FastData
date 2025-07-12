using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Structures;

internal sealed class ArrayStructure<TKey, TValue> : IStructure<TKey, TValue, ArrayContext<TKey, TValue>>
{
    public ArrayContext<TKey, TValue> Create(TKey[] data, ValueSpec<TValue>? valueSpec)
    {
        return new ArrayContext<TKey, TValue>(data, valueSpec);
    }
}