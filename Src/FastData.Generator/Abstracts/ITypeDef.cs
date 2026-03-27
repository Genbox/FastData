namespace Genbox.FastData.Generator.Abstracts;

public interface ITypeDef
{
    TypeCode KeyType { get; }
    string Name { get; }
    Func<TypeMap, object, string> PrintObj { get; }
}

public interface ITypeDef<in T> : ITypeDef
{
    Func<TypeMap, T, string> Print { get; }
}