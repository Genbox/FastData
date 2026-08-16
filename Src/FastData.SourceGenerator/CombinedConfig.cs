using Genbox.FastData.Config;
using Genbox.FastData.Generator.CSharp;

namespace Genbox.FastData.SourceGenerator;

internal sealed class CombinedConfig(Array keys, Array? values, DataConfig fdConfig, CSharpCodeGeneratorConfig csConfig, string incrementalKey) : IEquatable<CombinedConfig>
{
    public Array Keys { get; } = keys;
    public Array? Values { get; } = values;
    internal DataConfig FDConfig { get; } = fdConfig;
    internal CSharpCodeGeneratorConfig CSConfig { get; } = csConfig;
    private string IncrementalKey { get; } = incrementalKey;

    public bool Equals(CombinedConfig? other) => other != null && string.Equals(IncrementalKey, other.IncrementalKey, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is CombinedConfig other && Equals(other);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(IncrementalKey);
}