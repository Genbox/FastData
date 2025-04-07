using Genbox.FastData.Enums;

namespace Genbox.FastData.Extensions;

public static class KnownDataTypeExtensions
{
    public static bool IsUnsigned(this KnownDataType type) => type switch
    {
        KnownDataType.SByte or KnownDataType.Int16 or KnownDataType.Int32 or KnownDataType.Int64 or KnownDataType.Single or KnownDataType.Double => false,
        KnownDataType.UInt32 or KnownDataType.UInt16 or KnownDataType.UInt64 or KnownDataType.Byte or KnownDataType.Char => true,
        KnownDataType.Unknown or KnownDataType.String or KnownDataType.Boolean => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    public static bool IsInteger(this KnownDataType type) => type switch
    {
        KnownDataType.SByte or KnownDataType.Int16 or KnownDataType.Int32 or KnownDataType.Int64 or KnownDataType.Single or KnownDataType.Double or KnownDataType.UInt32 or KnownDataType.UInt16 or KnownDataType.UInt64 or KnownDataType.Byte or KnownDataType.Char => true,
        KnownDataType.String or KnownDataType.Boolean => false,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
}