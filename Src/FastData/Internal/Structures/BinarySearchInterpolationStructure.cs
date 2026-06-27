using Genbox.FastData.Config;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Structures;

public sealed class BinarySearchInterpolationStructure<TKey, TValue> : IStructure<TKey, TValue, BinarySearchInterpolationContext<TKey, TValue>>
{
    public StructureCapability SupportedCapabilities => StructureCapability.Membership | StructureCapability.KeyValueLookup | StructureCapability.Enumeration | StructureCapability.DirectAccess;

    public BinarySearchInterpolationContext<TKey, TValue> Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values) => new BinarySearchInterpolationContext<TKey, TValue>(keys, values);

    public IEnumerable<IEarlyExit> GetMandatoryExits() => [];
}