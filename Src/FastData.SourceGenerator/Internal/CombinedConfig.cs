using Genbox.FastData.Configs;
using Genbox.FastData.Generator.CSharp;

namespace Genbox.FastData.SourceGenerator.Internal;

internal class CombinedConfig(object[] data, FastDataConfig fastDataConfig, CSharpGeneratorConfig cSharpGeneratorConfig)
{
    public object[] Data { get; } = data;
    internal FastDataConfig FastDataConfig { get; } = fastDataConfig;
    internal CSharpGeneratorConfig CSharpGeneratorConfig { get; } = cSharpGeneratorConfig;
}