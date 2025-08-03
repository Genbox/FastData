using Genbox.FastData.Enums;

namespace Genbox.FastData.Generators.Extensions;

/// <summary>Provides extension methods for the <see cref="KeyType" /> enum.</summary>
public static class KeyTypeExtensions
{
    /// <summary>Determines whether the specified <see cref="KeyType" /> represents an integer type.</summary>
    /// <param name="type">The data type to check.</param>
    /// <returns><see langword="true" /> if the type is an integer type; otherwise, <see langword="false" />.</returns>
    public static bool IsInteger(this KeyType type) => type switch
    {
        KeyType.SByte or KeyType.Int16 or KeyType.Int32 or KeyType.Int64 or KeyType.Single or KeyType.Double or KeyType.UInt32 or KeyType.UInt16 or KeyType.UInt64 or KeyType.Byte or KeyType.Char => true,
        KeyType.String => false,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    /// <summary>Determines whether the specified <see cref="KeyType" /> uses identity hashing.</summary>
    /// <param name="type">The data type to check.</param>
    /// <returns><see langword="true" /> if the type uses identity hashing; otherwise, <see langword="false" />.</returns>
    public static bool IsIdentityHash(this KeyType type) => type switch
    {
        KeyType.Char or KeyType.SByte or KeyType.Byte or KeyType.Int16 or KeyType.UInt16 or KeyType.Int32 or KeyType.UInt32 or KeyType.Int64 or KeyType.UInt64 => true,
        KeyType.String or KeyType.Single or KeyType.Double => false,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
}