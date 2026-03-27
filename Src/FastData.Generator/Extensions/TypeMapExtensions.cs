using Genbox.FastData.Generator.Abstracts;
using Genbox.FastData.Generator.Definitions;

namespace Genbox.FastData.Generator.Extensions;

public static class TypeMapExtensions
{
    public static string ToValueLabel(this TypeMap map, object value, Type type)
    {
        ITypeDef s = map.Get(type);
        return s.PrintObj(map, value);
    }

    public static string ToValueLabel<TValue>(this TypeMap map, TValue value)
    {
        ITypeDef<TValue> s = map.Get<TValue>();
        return s.Print(map, value);
    }

    public static string GetObjectDeclaration<TValue>(this TypeMap map) => GetObjectDeclaration(map, typeof(TValue));

    public static string GetObjectDeclaration(this TypeMap map, Type valueType)
    {
        ITypeDef s = map.Get<object>();
        ObjectTypeDef def = (ObjectTypeDef)s;
        return def.PrintDeclaration(map, valueType);
    }

    public static string GetSmallestUIntType(this TypeMap map, ulong value) => value switch
    {
        <= byte.MaxValue => map.Get<byte>().Name,
        <= ushort.MaxValue => map.Get<ushort>().Name,
        <= uint.MaxValue => map.Get<uint>().Name,
        _ => map.Get<ulong>().Name
    };

    public static string GetSmallestIntType(this TypeMap map, long value) => value switch
    {
        <= sbyte.MaxValue => map.Get<sbyte>().Name,
        <= short.MaxValue => map.Get<short>().Name,
        <= int.MaxValue => map.Get<int>().Name,
        _ => map.Get<long>().Name
    };
}