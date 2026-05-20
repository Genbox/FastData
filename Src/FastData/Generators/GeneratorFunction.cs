namespace Genbox.FastData.Generators;

[Flags]
public enum GeneratorFunction
{
    None = 0,
    UnitAt = 1 << 1,
    UnitAtAsciiLower = 1 << 2,
    Length = 1 << 3,
    EqualsAt = 1 << 4,
    EqualsAtAsciiLower = 1 << 5,
    ReadU8 = 1 << 6,
    ReadU16 = 1 << 7,
    ReadU32 = 1 << 8,
    ReadU64 = 1 << 9,
    IsAsciiOnly = 1 << 10
}