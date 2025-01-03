using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Enums;

namespace Genbox.FastData.Internal;

internal sealed class FastDataSpec
{
    public string DataTypeName { get; set; } = null!;
    public bool IsArray { get; set; }
    public KnownDataType KnownDataType { get; set; }
    public string Name { get; set; } = null!;
    public object[] Data { get; set; } = null!;
    public StorageMode StorageMode { get; set; }
    public StorageOption StorageOptions { get; set; }
    public string? Namespace { get; set; }
    public ClassVisibility ClassVisibility { get; set; }
    public ClassType ClassType { get; set; }
}