using Genbox.FastData.Enums;

namespace Genbox.FastData.Generators.Extensions;

/// <summary>Provides extension methods for the <see cref="DataType" /> enum.</summary>
public static class DataTypeExtensions
{
    /// <summary>Determines whether the specified <see cref="DataType" /> represents an integer type.</summary>
    /// <param name="type">The data type to check.</param>
    /// <returns><see langword="true" /> if the type is an integer type; otherwise, <see langword="false" />.</returns>
    public static bool IsInteger(this DataType type) => type switch
    {
        DataType.SByte or DataType.Int16 or DataType.Int32 or DataType.Int64 or DataType.Single or DataType.Double or DataType.UInt32 or DataType.UInt16 or DataType.UInt64 or DataType.Byte or DataType.Char => true,
        DataType.String => false,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    /// <summary>Determines whether the specified <see cref="DataType" /> uses identity hashing.</summary>
    /// <param name="type">The data type to check.</param>
    /// <returns><see langword="true" /> if the type uses identity hashing; otherwise, <see langword="false" />.</returns>
    public static bool IsIdentityHash(this DataType type) => type switch
    {
        DataType.Char or DataType.SByte or DataType.Byte or DataType.Int16 or DataType.UInt16 or DataType.Int32 or DataType.UInt32 or DataType.Int64 or DataType.UInt64 => true,
        DataType.String or DataType.Single or DataType.Double => false,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
}