using Genbox.FastEnum;

namespace Genbox.FastData.Internal.Enums;

[FastEnum]
public enum KnownDataType
{
    Unknown = 0,
    String,
    Boolean,
    SByte,
    Byte,
    Char,
    Int16,
    UInt16,
    Int32,
    UInt32,
    Int64,
    UInt64,
    Single,
    Double,
}