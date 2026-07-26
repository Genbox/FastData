using Genbox.FastData.Generators.Helpers;
using JetBrains.Annotations;

namespace Genbox.FastData.Generator.Helpers;

[PublicAPI]
public static class IntegralValueConverter
{
    public static object ConvertChecked(object value, Type targetType)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value), "The value cannot be null.");

        if (targetType == null)
            throw new ArgumentNullException(nameof(targetType), "The target type cannot be null.");

        Type sourceType = value.GetType();

        return IntegralTypeHelper.IsSigned(sourceType)
            ? ConvertSigned(IntegralTypeHelper.GetSignedValue(value), targetType)
            : ConvertUnsigned(IntegralTypeHelper.GetUnsignedValue(value), targetType);
    }

    private static object ConvertSigned(long value, Type targetType)
    {
        checked
        {
            if (targetType == typeof(char))
                return (char)value;
            if (targetType == typeof(sbyte))
                return (sbyte)value;
            if (targetType == typeof(byte))
                return (byte)value;
            if (targetType == typeof(short))
                return (short)value;
            if (targetType == typeof(ushort))
                return (ushort)value;
            if (targetType == typeof(int))
                return (int)value;
            if (targetType == typeof(uint))
                return (uint)value;
            if (targetType == typeof(long))
                return value;
            if (targetType == typeof(ulong))
                return (ulong)value;
        }

        throw new InvalidOperationException("The validated target type could not be converted.");
    }

    private static object ConvertUnsigned(ulong value, Type targetType)
    {
        checked
        {
            if (targetType == typeof(char))
                return (char)value;
            if (targetType == typeof(sbyte))
                return (sbyte)value;
            if (targetType == typeof(byte))
                return (byte)value;
            if (targetType == typeof(short))
                return (short)value;
            if (targetType == typeof(ushort))
                return (ushort)value;
            if (targetType == typeof(int))
                return (int)value;
            if (targetType == typeof(uint))
                return (uint)value;
            if (targetType == typeof(long))
                return (long)value;
            if (targetType == typeof(ulong))
                return value;
        }

        throw new InvalidOperationException("The validated target type could not be converted.");
    }
}