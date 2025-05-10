using Genbox.FastData.Enums;

namespace Genbox.FastData.Generator.Framework.Interfaces.Specs;

public interface ITypeSpec
{
    DataType DataType { get; }
    string Name { get; }
}

// ReSharper disable once UnusedTypeParameter
#pragma warning disable S2326
public interface ITypeSpec<in T> : ITypeSpec
#pragma warning restore S2326
{ }