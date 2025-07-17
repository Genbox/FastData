using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Framework.Definitions;
using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generators;

namespace Genbox.FastData.Generator.Extensions;

public static class TypeMapExtensions
{
    public static string ToValueLabel(this TypeMap map, object value, Type type)
    {
        ITypeDef s = map.Get(type);
        return s.PrintObj(map, value);
    }

    public static string ToValueLabel<T>(this TypeMap map, T value)
    {
        ITypeDef<T> s = map.Get<T>();
        return s.Print(map, value);
    }

    public static string GetDeclarations<TValue>(this TypeMap map)
    {
        ITypeDef s = map.Get<object>();
        ObjectTypeDef def = (ObjectTypeDef)s;
        return def.PrintDeclaration(map, typeof(TValue)); //TODO: Enumerate all types
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