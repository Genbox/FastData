using Genbox.FastData.Config;
using Genbox.FastData.Generator.CSharp;

namespace Genbox.FastData.SourceGenerator;

internal class CombinedConfig(Array keys, Array? values, DataConfig fdConfig, CSharpCodeGeneratorConfig csConfig)
{
    public Array Keys { get; } = keys;
    public Array? Values { get; } = values;
    internal DataConfig FDConfig { get; } = fdConfig;
    internal CSharpCodeGeneratorConfig CSConfig { get; } = csConfig;
}