namespace Genbox.FastData.Generators.Helpers;

/// <summary>Provides integral type classification and value conversion for generators.</summary>
public static class IntegralTypeHelper
{
    /// <summary>Reports whether the type is a supported signed integer type.</summary>
    public static bool IsSigned(Type type) => type == typeof(sbyte)
                                              || type == typeof(short)
                                              || type == typeof(int)
                                              || type == typeof(long);

    internal static bool IsUnsigned(Type type) => type == typeof(byte)
                                                  || type == typeof(ushort)
                                                  || type == typeof(uint)
                                                  || type == typeof(ulong);

    /// <summary>Converts a boxed signed integer to Int64.</summary>
    public static long GetSignedValue(object value) => value switch
    {
        sbyte item => item,
        short item => item,
        int item => item,
        long item => item,
        _ => throw new ArgumentException("The value must be a supported signed integral type.", nameof(value))
    };

    /// <summary>Converts a boxed unsigned integer or character to UInt64.</summary>
    public static ulong GetUnsignedValue(object value) => value switch
    {
        char item => item,
        byte item => item,
        ushort item => item,
        uint item => item,
        ulong item => item,
        _ => throw new ArgumentException("The value must be a supported unsigned integral type.", nameof(value))
    };
}