namespace Genbox.FastData.InternalShared;

public readonly struct GeneratorSpec(string identifier, string source)
{
    public string Identifier { get; } = identifier;
    public string Source { get; } = source;
}