using Genbox.FastData.Enums;

namespace Genbox.FastData.Configs;

public class FastDataConfig(string name, object[] data, StructureType structureType = StructureType.Auto)
{
    public string Name { get; set; } = name;
    public object[] Data { get; set; } = data;

    public StructureType StructureType { get; set; } = structureType;
    public StorageOption StorageOptions { get; set; }
    public AnalyzerConfig? AnalyzerConfig { get; set; }
    public StringComparison StringComparison { get; set; } = StringComparison.Ordinal;
}