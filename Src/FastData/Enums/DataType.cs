namespace Genbox.FastData.Enums;

public enum DataType : byte
{
    //The names and values in here must match TypeCode

    Null = 0,

    Object = 1,
    // DBNull = 2,
    // Boolean = 3,
    Char = 4,
    SByte = 5,
    Byte = 6,
    Int16 = 7,
    UInt16 = 8,
    Int32 = 9,
    UInt32 = 10,
    Int64 = 11,
    UInt64 = 12,
    Single = 13,
    Double = 14,

    // Decimal = 15,
    // DateTime = 16,
    String = 18
}