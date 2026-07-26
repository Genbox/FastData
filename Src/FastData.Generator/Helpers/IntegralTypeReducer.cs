using Genbox.FastData.Generators.Helpers;
using JetBrains.Annotations;

namespace Genbox.FastData.Generator.Helpers;

[PublicAPI]
public static class IntegralTypeReducer
{
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

    public static Type GetSmallestUnsignedStorageType(ulong maxValue) => maxValue switch
    {
        <= byte.MaxValue => typeof(byte),
        <= ushort.MaxValue => typeof(ushort),
        <= uint.MaxValue => typeof(uint),
        _ => typeof(ulong)
    };

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
                long signedValue = IntegralTypeHelper.GetSignedValue(value!);
                if (signedValue < 0)
                    return sourceType;

                unsignedValue = (ulong)signedValue;
            }
            else
                unsignedValue = IntegralTypeHelper.GetUnsignedValue(value!);

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