using Genbox.FastData.Generators;

namespace Genbox.FastData.InternalShared;

public readonly struct GeneratorSpec(string identifier, string source, GeneratorFlags flags)
{
    public string Identifier { get; } = identifier;
    public string Source { get; } = source;
    public GeneratorFlags Flags { get; } = flags;
}