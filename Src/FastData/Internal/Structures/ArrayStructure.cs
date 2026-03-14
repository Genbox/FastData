using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Structures;

public sealed class ArrayStructure<TKey, TValue> : IStructure<TKey, TValue, ArrayContext<TKey, TValue>>
{
    public ArrayContext<TKey, TValue> Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values) => new ArrayContext<TKey, TValue>(keys, values);
}