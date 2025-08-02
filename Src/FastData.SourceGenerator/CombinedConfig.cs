using Genbox.FastData.Generator.CSharp;

namespace Genbox.FastData.SourceGenerator;

internal class CombinedConfig(Array keys, Array? values, FastDataConfig fdConfig, CSharpCodeGeneratorConfig csConfig)
{
    public Array Keys { get; } = keys;
    public Array? Values { get; } = values;
    internal FastDataConfig FDConfig { get; } = fdConfig;
    internal CSharpCodeGeneratorConfig CSConfig { get; } = csConfig;
}