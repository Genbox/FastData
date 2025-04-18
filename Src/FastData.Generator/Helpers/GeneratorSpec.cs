using Genbox.FastData.Enums;

namespace Genbox.FastData.Generator.Helpers;

public readonly struct GeneratorSpec(string identifier, DataType dataType, string source)
{
    public string Identifier { get; } = identifier;
    public DataType DataType { get; } = dataType;
    public string Source { get; } = source;
}