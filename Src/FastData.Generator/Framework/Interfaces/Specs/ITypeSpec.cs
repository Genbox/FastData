using Genbox.FastData.Enums;

namespace Genbox.FastData.Generator.Framework.Interfaces.Specs;

public interface ITypeSpec
{
    DataType DataType { get; }
    string Name { get; }
}

public interface ITypeSpec<in T> : ITypeSpec
{
    Func<T, string> Print { get; }
}