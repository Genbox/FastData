#if NETSTANDARD2_0
using System.Runtime.Serialization;
#else
using System.Runtime.CompilerServices;
#endif
using Genbox.FastData.Config;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal;

internal static class StructureCapabilityHelper
{
    internal static bool Supports(Type structureType, StructureCapability structureCapability) => (GetStructureCapability(structureType) & structureCapability) == structureCapability;

    internal static StructureCapability GetStructureCapability(Type structureType)
    {
        Type concreteType = structureType.ContainsGenericParameters ? structureType.MakeGenericType(typeof(int), typeof(int)) : structureType;

        if (CreateUninitialized(concreteType) is not IStructure structure)
            throw new InvalidOperationException($"Structure {structureType.Name} does not implement {nameof(IStructure)}.");

        return structure.SupportedCapabilities;
    }

    private static object CreateUninitialized(Type type)
    {
#if NETSTANDARD2_0
        return FormatterServices.GetUninitializedObject(type);
#else
        return RuntimeHelpers.GetUninitializedObject(type);
#endif
    }
}