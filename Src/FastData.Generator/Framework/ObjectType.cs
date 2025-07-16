using Genbox.FastData.Generator.Framework.Models;

namespace Genbox.FastData.Generator.Framework;

public class ObjectType(Type type, ValuesModel model)
{
    public static ObjectType Create<TValue>(TValue[] values, TypeMap typeMap)
    {
        ValuesModel model = ModelBuilder.Build(values, typeMap);
        return new ObjectType(typeof(TValue), model);
    }

    public bool IsCustomType => Type.GetTypeCode(type) == TypeCode.Object;
    public ValuesModel Model { get; } = model;
}