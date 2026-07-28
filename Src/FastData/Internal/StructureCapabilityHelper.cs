using Genbox.FastData.Config;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Structures;
#if NETSTANDARD2_0
using System.Runtime.Serialization;
#else
using System.Runtime.CompilerServices;
#endif

namespace Genbox.FastData.Internal;

internal static class StructureCapabilityHelper
{
    internal static bool Supports(StructureType structureType, StructureCapability structureCapability) => (GetStructureCapability(structureType) & structureCapability) == structureCapability;

    internal static StructureCapability GetStructureCapability(StructureType structureType)
    {
        Type type = GetRuntimeType(structureType);
        Type concreteType = type.ContainsGenericParameters ? type.MakeGenericType(typeof(int), typeof(int)) : type;

        if (CreateUninitialized(concreteType) is not IStructure structure)
            throw new InvalidOperationException($"Structure {type.Name} does not implement {nameof(IStructure)}.");

        return structure.SupportedCapabilities;
    }

    private static Type GetRuntimeType(StructureType structureType) => structureType switch
    {
        StructureType.Array => typeof(ArrayStructure<,>),
        StructureType.BinarySearch => typeof(BinarySearchStructure<,>),
        StructureType.BinarySearchInterpolation => typeof(BinarySearchInterpolationStructure<,>),
        StructureType.BitSet => typeof(BitSetStructure<,>),
        StructureType.BloomFilter => typeof(BloomFilterStructure<,>),
        StructureType.Conditional => typeof(ConditionalStructure<,>),
        StructureType.ConstMap => typeof(ConstMapStructure<,>),
        StructureType.EliasFano => typeof(EliasFanoStructure<,>),
        StructureType.HashTableCompact => typeof(HashTableCompactStructure<,>),
        StructureType.HashTablePerfect => typeof(HashTablePerfectStructure<,>),
        StructureType.HashTable => typeof(HashTableStructure<,>),
        StructureType.Hyble => typeof(HybleStructure<,>),
        StructureType.KeyLength => typeof(KeyLengthStructure<,>),
        StructureType.Pgm => typeof(PgmStructure<,>),
        StructureType.Range => typeof(RangeStructure<,>),
        StructureType.RrrBitVector => typeof(RrrBitVectorStructure<,>),
        StructureType.SingleValue => typeof(SingleValueStructure<,>),
        StructureType.Auto => throw new ArgumentException("Automatic structure selection has no fixed capabilities.", nameof(structureType)),
        StructureType.None => throw new ArgumentException("No data structure has no fixed capabilities.", nameof(structureType)),
        _ => throw new ArgumentOutOfRangeException(nameof(structureType), structureType, "Unsupported structure type.")
    };

    private static object CreateUninitialized(Type type)
    {
#if NETSTANDARD2_0
        return FormatterServices.GetUninitializedObject(type);
#else
        return RuntimeHelpers.GetUninitializedObject(type);
#endif
    }
}