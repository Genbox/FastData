using Genbox.FastData.Generator.CSharp;

namespace Genbox.FastData.SourceGenerator.Internal;

internal class CombinedConfig(object[] data, FastDataConfig fdConfig, CSharpCodeGeneratorConfig csConfig)
{
    public object[] Data { get; } = data;
    internal FastDataConfig FDConfig { get; } = fdConfig;
    internal CSharpCodeGeneratorConfig CSConfig { get; } = csConfig;
}