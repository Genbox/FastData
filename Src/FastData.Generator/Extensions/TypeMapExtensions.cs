using System.Globalization;
using Genbox.FastData.Generator.Abstracts;
using Genbox.FastData.Generator.Definitions;
using JetBrains.Annotations;

namespace Genbox.FastData.Generator.Extensions;

[PublicAPI]
public static class TypeMapExtensions
{
    public static string ToValueLabel(this TypeMap map, object value, Type type)
    {
        ITypeDef s = map.Get(type);
        object converted = value.GetType() == type ? value : Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        return s.PrintObj(map, converted);
    }

    public static string ToValueLabel<TValue>(this TypeMap map, TValue value)
    {
        ITypeDef<TValue> s = map.Get<TValue>();
        return s.Print(map, value);
    }

    public static string GetObjectDeclaration<TValue>(this TypeMap map) => map.GetObjectDeclaration(typeof(TValue));

    public static string GetObjectDeclaration(this TypeMap map, Type valueType)
    {
        ITypeDef s = map.Get<object>();
        ObjectTypeDef def = (ObjectTypeDef)s;
        return def.PrintDeclaration(map, valueType);
    }

    public static string GetSmallestUnsignedTypeName(this TypeMap map, ulong value) => map.GetTypeName(map.GetSmallestUnsignedType(value));

    public static string GetSmallestSignedTypeName(this TypeMap map, long value) => map.GetSmallestSignedTypeName(value, value);

    public static string GetSmallestSignedTypeName(this TypeMap map, long minValue, long maxValue) => map.GetTypeName(map.GetSmallestSignedType(minValue, maxValue));

    public static Type GetSmallestIntegerType(this TypeMap map, Type sourceType, object minValue, object maxValue) => Type.GetTypeCode(sourceType) switch
    {
        TypeCode.SByte or TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64 => map.GetSmallestSignedType(Convert.ToInt64(minValue, CultureInfo.InvariantCulture), Convert.ToInt64(maxValue, CultureInfo.InvariantCulture)),
        TypeCode.Char or TypeCode.Byte or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64 => map.GetSmallestUnsignedType(Convert.ToUInt64(maxValue, CultureInfo.InvariantCulture)),
        _ => throw new ArgumentException("The source type must be an integral type.", nameof(sourceType))
    };

    public static Type GetSmallestNonNegativeIntegerType(this TypeMap map, Type sourceType, Array values)
    {
        TypeCode typeCode = Type.GetTypeCode(sourceType);
        bool signed = typeCode is TypeCode.SByte or TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64;
        bool unsigned = typeCode is TypeCode.Char or TypeCode.Byte or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64;

        if (!signed && !unsigned)
            return sourceType;

        ulong maxValue = 0;
        foreach (object value in values)
        {
            ulong unsignedValue;
            if (signed)
            {
                long signedValue = Convert.ToInt64(value, CultureInfo.InvariantCulture);
                if (signedValue < 0)
                    return sourceType;

                unsignedValue = (ulong)signedValue;
            }
            else
                unsignedValue = Convert.ToUInt64(value, CultureInfo.InvariantCulture);

            if (unsignedValue > maxValue)
                maxValue = unsignedValue;
        }

        return map.GetSmallestUnsignedType(maxValue);
    }

    public static Type GetSmallestUnsignedType(this TypeMap _, ulong maxValue) => maxValue switch
    {
        <= byte.MaxValue => typeof(byte),
        <= ushort.MaxValue => typeof(ushort),
        <= uint.MaxValue => typeof(uint),
        _ => typeof(ulong)
    };

    public static Type GetSmallestSignedType(this TypeMap _, long minValue, long maxValue)
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
}