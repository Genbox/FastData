namespace Genbox.FastData.Generators.Helpers;

internal static class IntegralTypeHelper
{
    internal static bool IsSigned(Type type) => type == typeof(sbyte)
                                                || type == typeof(short)
                                                || type == typeof(int)
                                                || type == typeof(long);

    internal static bool IsUnsigned(Type type) => type == typeof(byte)
                                                  || type == typeof(ushort)
                                                  || type == typeof(uint)
                                                  || type == typeof(ulong);

    internal static long GetSignedValue(object value) => value switch
    {
        sbyte item => item,
        short item => item,
        int item => item,
        long item => item,
        _ => throw new ArgumentException("The value must be a supported signed integral type.", nameof(value))
    };

    internal static ulong GetUnsignedValue(object value) => value switch
    {
        char item => item,
        byte item => item,
        ushort item => item,
        uint item => item,
        ulong item => item,
        _ => throw new ArgumentException("The value must be a supported unsigned integral type.", nameof(value))
    };
}