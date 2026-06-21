using Genbox.FastData.Config;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Structures;

public sealed class BinarySearchStructure<TKey, TValue> : IStructure<TKey, TValue, BinarySearchContext<TKey, TValue>>
{
    public StructureCapability SupportedCapabilities => StructureCapability.Membership | StructureCapability.KeyValueLookup | StructureCapability.Enumeration | StructureCapability.DirectAccess;

    public BinarySearchContext<TKey, TValue> Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values)
    {
        return new BinarySearchContext<TKey, TValue>(keys, values);
    }

    public IEnumerable<IEarlyExit> GetMandatoryExits() => [];
}