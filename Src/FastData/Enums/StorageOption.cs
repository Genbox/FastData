namespace Genbox.FastData.Enums;

[Flags]
public enum StorageOption
{
    None = 0,
    OptimizeForMemory = 1,
    OptimizeForSpeed = 2,
    Enable64BitHashing = 4
}