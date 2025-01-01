using Genbox.FastData.Enums;

namespace Genbox.FastData.Internal;

internal sealed class FastDataSpec
{
    public string Name { get; set; } = null!;
    public string[] Data { get; set; } = null!;
    public StorageMode StorageMode { get; set; }
    public string? Namespace { get; set; }
    public ClassVisibility ClassVisibility { get; set; }
    public ClassType ClassType { get; set; }
}