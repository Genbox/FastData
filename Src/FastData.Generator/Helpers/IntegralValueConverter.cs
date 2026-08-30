using Genbox.FastData.Generators.Helpers;
using JetBrains.Annotations;

namespace Genbox.FastData.Generator.Helpers;

/// <summary>Converts boxed integral values with checked overflow semantics.</summary>
[PublicAPI]
public static class IntegralValueConverter
{
    /// <summary>Converts a boxed integral value to another integral type.</summary>
    /// <param name="value">The boxed integral value to convert.</param>
    /// <param name="targetType">The destination CLR integral type.</param>
    /// <returns>The converted boxed value.</returns>
    /// <exception cref="OverflowException">The value cannot be represented by <paramref name="targetType"/>.</exception>
    public static object ConvertChecked(object value, Type targetType)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value), "The value cannot be null.");

        if (targetType == null)
            throw new ArgumentNullException(nameof(targetType), "The target type cannot be null.");

        Type sourceType = value.GetType();

        if (IntegralTypeHelper.IsSigned(sourceType))
        {
            long signedValue = IntegralTypeHelper.GetSignedValue(value);
            checked
            {
                if (targetType == typeof(char))
                    return (char)signedValue;
                if (targetType == typeof(sbyte))
                    return (sbyte)signedValue;
                if (targetType == typeof(byte))
                    return (byte)signedValue;
                if (targetType == typeof(short))
                    return (short)signedValue;
                if (targetType == typeof(ushort))
                    return (ushort)signedValue;
                if (targetType == typeof(int))
                    return (int)signedValue;
                if (targetType == typeof(uint))
                    return (uint)signedValue;
                if (targetType == typeof(long))
                    return signedValue;
                if (targetType == typeof(ulong))
                    return (ulong)signedValue;
            }
        }
        else
        {
            ulong unsignedValue = IntegralTypeHelper.GetUnsignedValue(value);
            checked
            {
                if (targetType == typeof(char))
                    return (char)unsignedValue;
                if (targetType == typeof(sbyte))
                    return (sbyte)unsignedValue;
                if (targetType == typeof(byte))
                    return (byte)unsignedValue;
                if (targetType == typeof(short))
                    return (short)unsignedValue;
                if (targetType == typeof(ushort))
                    return (ushort)unsignedValue;
                if (targetType == typeof(int))
                    return (int)unsignedValue;
                if (targetType == typeof(uint))
                    return (uint)unsignedValue;
                if (targetType == typeof(long))
                    return (long)unsignedValue;
                if (targetType == typeof(ulong))
                    return unsignedValue;
            }
        }

        throw new InvalidOperationException("The validated target type could not be converted.");
    }
}