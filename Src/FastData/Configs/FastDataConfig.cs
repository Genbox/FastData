using Genbox.FastData.Abstracts;
using Genbox.FastData.Enums;

namespace Genbox.FastData.Configs;

public class FastDataConfig(StructureType structureType = StructureType.Auto)
{
    public StructureType StructureType { get; set; } = structureType;
    public StorageOption StorageOptions { get; set; }
    public SimulatorConfig SimulatorConfig { get; set; } = new SimulatorConfig();
    public IAnalyzerConfig? AnalyzerConfig { get; set; }
    public StringComparison StringComparison { get; set; } = StringComparison.Ordinal;
}