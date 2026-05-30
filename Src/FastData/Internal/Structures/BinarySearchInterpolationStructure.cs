using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Structures;

public sealed class BinarySearchInterpolationStructure<TKey, TValue> : IStructure<TKey, TValue, BinarySearchInterpolationContext<TKey, TValue>>
{
    public BinarySearchInterpolationContext<TKey, TValue> Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values)
    {
        return new BinarySearchInterpolationContext<TKey, TValue>(keys, values);
    }

    public IEnumerable<IEarlyExit> GetMandatoryExits() => [];
}