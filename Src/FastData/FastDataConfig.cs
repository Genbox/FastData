using Genbox.FastData.Enums;
using JetBrains.Annotations;

namespace Genbox.FastData;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed class FastDataConfig(StructureType structureType = StructureType.Auto)
{
    public StructureType StructureType { get; set; } = structureType;
    public int HashCapacityFactor { get; set; } = 1;
}