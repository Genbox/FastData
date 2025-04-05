using Genbox.FastData.Enums;

namespace Genbox.FastData.Generator.Helpers;

public readonly struct GeneratorSpec(string identifier, KnownDataType dataType, string source)
{
    public string Identifier { get; } = identifier;
    public KnownDataType DataType { get; } = dataType;
    public string Source { get; } = source;
}