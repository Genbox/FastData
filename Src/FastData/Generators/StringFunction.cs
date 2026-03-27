namespace Genbox.FastData.Generators;

[Flags]
public enum StringFunction
{
    None = 0,
    ReadU8 = 1 << 1,
    ReadU16 = 1 << 2,
    ReadU32 = 1 << 3,
    ReadU64 = 1 << 4,
    GetFirstChar = 1 << 5,
    GetFirstCharLower = 1 << 6,
    GetLastChar = 1 << 7,
    GetLastCharLower = 1 << 8,
    GetLength = 1 << 9,
    StartsWith = 1 << 10,
    StartsWithLower = 1 << 11,
    EndsWith = 1 << 12,
    EndsWithLower = 1 << 13
}