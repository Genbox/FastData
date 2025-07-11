using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Framework.Interfaces;

namespace Genbox.FastData.Generator.Extensions;

public static class TypeMapExtensions
{
    public static string GetTypeName(this TypeMap map, Type type) => map.Get(type).Name;

    public static string ToValueLabel(this TypeMap map, object value, Type type)
    {
        ITypeDef s = map.Get(type);
        return s.PrintObj(value);
    }

    public static string ToValueLabel<T>(this TypeMap map, T value)
    {
        ITypeDef<T> s = map.Get<T>();
        return s.Print(value);
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