using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Structures;

internal sealed class ArrayStructure<TKey, TValue> : IStructure<TKey, TValue, ArrayContext<TKey, TValue>>
{
    public ArrayContext<TKey, TValue> Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values)
    {
        return new ArrayContext<TKey, TValue>(keys, values);
    }
}