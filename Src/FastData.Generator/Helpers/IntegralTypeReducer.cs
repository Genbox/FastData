using Genbox.FastData.Generators.Helpers;
using JetBrains.Annotations;

namespace Genbox.FastData.Generator.Helpers;

/// <summary>Selects the smallest integral storage type that can represent a set of values.</summary>
[PublicAPI]
public static class IntegralTypeReducer
{
    /// <summary>Gets the smallest signed integral type that contains the specified range.</summary>
    /// <param name="minValue">The minimum value in the range.</param>
    /// <param name="maxValue">The maximum value in the range.</param>
    /// <returns>The smallest signed CLR integral type that contains the range.</returns>
    public static Type GetSmallestSignedStorageType(long minValue, long maxValue)
    {
        if (minValue > maxValue)
            throw new ArgumentException("The minimum value cannot be greater than the maximum value.", nameof(minValue));

        if (minValue >= sbyte.MinValue && maxValue <= sbyte.MaxValue)
            return typeof(sbyte);

        if (minValue >= short.MinValue && maxValue <= short.MaxValue)
            return typeof(short);

        if (minValue >= int.MinValue && maxValue <= int.MaxValue)
            return typeof(int);

        return typeof(long);
    }

    /// <summary>Gets the smallest unsigned integral type that contains values through the specified maximum.</summary>
    /// <param name="maxValue">The largest value that must be represented.</param>
    /// <returns>The smallest unsigned CLR integral type that contains the value.</returns>
    public static Type GetSmallestUnsignedStorageType(ulong maxValue) => maxValue switch
    {
        <= byte.MaxValue => typeof(byte),
        <= ushort.MaxValue => typeof(ushort),
        <= uint.MaxValue => typeof(uint),
        _ => typeof(ulong)
    };

    /// <summary>Gets the smallest integral type that contains a boxed range of the specified source type.</summary>
    /// <param name="sourceType">The CLR type of both boxed range values.</param>
    /// <param name="minValue">The boxed minimum value.</param>
    /// <param name="maxValue">The boxed maximum value.</param>
    /// <returns>The smallest CLR integral type that contains the range.</returns>
    public static Type GetSmallestStorageType(Type sourceType, object minValue, object maxValue)
    {
        if (sourceType == null)
            throw new ArgumentNullException(nameof(sourceType), "The source type cannot be null.");

        ValidateValueType(minValue, sourceType, nameof(minValue));
        ValidateValueType(maxValue, sourceType, nameof(maxValue));

        if (IntegralTypeHelper.IsSigned(sourceType))
            return GetSmallestSignedStorageType(IntegralTypeHelper.GetSignedValue(minValue), IntegralTypeHelper.GetSignedValue(maxValue));

        ulong unsignedMinValue = IntegralTypeHelper.GetUnsignedValue(minValue);
        ulong unsignedMaxValue = IntegralTypeHelper.GetUnsignedValue(maxValue);

        if (unsignedMinValue > unsignedMaxValue)
            throw new ArgumentException("The minimum value cannot be greater than the maximum value.", nameof(minValue));

        return GetSmallestUnsignedStorageType(unsignedMaxValue);
    }

    /// <summary>Gets the smallest unsigned type that contains all values, or the source type when a value is negative.</summary>
    /// <param name="sourceType">The CLR element type of <paramref name="values"/>.</param>
    /// <param name="values">The integral values to inspect.</param>
    /// <returns>The reduced unsigned storage type, or <paramref name="sourceType"/> when reduction is unsafe.</returns>
    public static Type GetSmallestNonNegativeStorageType(Type sourceType, Array values)
    {
        if (sourceType == null)
            throw new ArgumentNullException(nameof(sourceType), "The source type cannot be null.");

        if (values == null)
            throw new ArgumentNullException(nameof(values), "The values array cannot be null.");

        if (values.GetType().GetElementType() != sourceType)
            throw new ArgumentException("The array element type must exactly match the source type.", nameof(values));

        if (values.Length == 0)
            return sourceType;

        ulong maxValue = 0;

        foreach (object? value in values)
        {
            ValidateValueType(value, sourceType, nameof(values));

            ulong unsignedValue;
            if (IntegralTypeHelper.IsSigned(sourceType))
            {
                long signedValue = IntegralTypeHelper.GetSignedValue(value);
                if (signedValue < 0)
                    return sourceType;

                unsignedValue = (ulong)signedValue;
            }
            else
                unsignedValue = IntegralTypeHelper.GetUnsignedValue(value);

            if (unsignedValue > maxValue)
                maxValue = unsignedValue;
        }

        return GetSmallestUnsignedStorageType(maxValue);
    }

    private static void ValidateValueType(object? value, Type sourceType, string parameterName)
    {
        if (value == null)
            throw new ArgumentNullException(parameterName, "Integral values cannot be null.");

        if (value.GetType() != sourceType)
            throw new ArgumentException($"The boxed value type must exactly match '{sourceType}'.", parameterName);
    }
}