using Genbox.FastData.Enums;

namespace Genbox.FastData.Generator.Framework.Interfaces;

public interface ITypeDef
{
    KeyType KeyType { get; }
    string Name { get; }
    Func<TypeMap, object, string> PrintObj { get; }
}

public interface ITypeDef<in T> : ITypeDef
{
    Func<TypeMap, T, string> Print { get; }
}