using Genbox.FastData.Enums;
using JetBrains.Annotations;

namespace Genbox.FastData.Configs;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed class FastDataConfig(StructureType structureType = StructureType.Auto)
{
    public StructureType StructureType { get; set; } = structureType;
    public StorageOption StorageOptions { get; set; }
    public SimulatorConfig SimulatorConfig { get; set; } = new SimulatorConfig();
    public StringComparison StringComparison { get; set; } = StringComparison.Ordinal;
}