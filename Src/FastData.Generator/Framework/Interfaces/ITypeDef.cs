using Genbox.FastData.Enums;

namespace Genbox.FastData.Generator.Framework.Interfaces;

public interface ITypeDef
{
    DataType DataType { get; }
    string Name { get; }
}

public interface ITypeDef<in T> : ITypeDef
{
    Func<T, string> Print { get; }
}