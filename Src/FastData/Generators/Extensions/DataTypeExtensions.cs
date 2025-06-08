using Genbox.FastData.Enums;

namespace Genbox.FastData.Generators.Extensions;

public static class DataTypeExtensions
{
    public static bool IsInteger(this DataType type) => type switch
    {
        DataType.SByte or DataType.Int16 or DataType.Int32 or DataType.Int64 or DataType.Single or DataType.Double or DataType.UInt32 or DataType.UInt16 or DataType.UInt64 or DataType.Byte or DataType.Char => true,
        DataType.String or DataType.Boolean => false,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    public static bool IsIdentityHash(this DataType type) => type switch
    {
        DataType.Char or DataType.SByte or DataType.Byte or DataType.Int16 or DataType.UInt16 or DataType.Int32 or DataType.UInt32 or DataType.Int64 or DataType.UInt64 => true,
        DataType.Boolean or DataType.String or DataType.Single or DataType.Double => false,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
}