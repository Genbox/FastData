namespace Genbox.FastData.Generators.StringHash.Framework;

[Flags]
public enum ReaderFunctions : byte
{
    None = 0,
    ReadU8 = 1,
    ReadU16 = 2,
    ReadU32 = 4,
    ReadU64 = 8,
    All = ReadU8 | ReadU16 | ReadU32 | ReadU64
}