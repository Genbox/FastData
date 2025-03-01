using Genbox.FastData.Enums;

namespace Genbox.FastData;

public sealed class FastDataConfig
{
    public FastDataConfig(string name, object[] data)
    {
        Name = name;
        Data = data;
        DataType = (KnownDataType)Enum.Parse(typeof(KnownDataType), data[0].GetType().Name);
    }

    public string Name { get; set; }
    public object[] Data { get; set; }

    public KnownDataType DataType { get; }
    public StorageMode StorageMode { get; set; }
    public StorageOption StorageOptions { get; set; }
    public AnalyzerConfig? AnalyzerConfig { get; set; }
    public string? Namespace { get; set; }
    public ClassVisibility ClassVisibility { get; set; }
    public ClassType ClassType { get; set; }
}