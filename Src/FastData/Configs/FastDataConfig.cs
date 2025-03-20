using Genbox.FastData.Enums;

namespace Genbox.FastData.Configs;

public class FastDataConfig(string name, object[] data)
{
    public string Name { get; set; } = name;
    public object[] Data { get; set; } = data;

    public StorageMode StorageMode { get; set; }
    public StorageOption StorageOptions { get; set; }
    public AnalyzerConfig? AnalyzerConfig { get; set; }
    public StringComparison StringComparison { get; set; } = StringComparison.Ordinal;

    public KnownDataType GetDataType() => (KnownDataType)Enum.Parse(typeof(KnownDataType), Data[0].GetType().Name);
}