using Genbox.FastData.Generator.Framework.Interfaces;

namespace Genbox.FastData.Generator.Framework;

public class TypeHelper(TypeMap typeMap)
{
    public string GetTypeName(Type type) => typeMap.GetName(type);

    public string ToValueLabel<T>(T value)
    {
        ITypeDef<T> s = typeMap.Get<T>();
        return s.Print(value);
    }

    public string GetSmallestUIntType(ulong value) => value switch
    {
        <= byte.MaxValue => typeMap.Get<byte>().Name,
        <= ushort.MaxValue => typeMap.Get<ushort>().Name,
        <= uint.MaxValue => typeMap.Get<uint>().Name,
        _ => typeMap.Get<ulong>().Name
    };

    public string GetSmallestIntType(long value) => value switch
    {
        <= sbyte.MaxValue => typeMap.Get<sbyte>().Name,
        <= short.MaxValue => typeMap.Get<short>().Name,
        <= int.MaxValue => typeMap.Get<int>().Name,
        _ => typeMap.Get<long>().Name
    };
}