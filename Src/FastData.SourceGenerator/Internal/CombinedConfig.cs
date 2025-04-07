using Genbox.FastData.Configs;
using Genbox.FastData.Generator.CSharp;

namespace Genbox.FastData.SourceGenerator.Internal;

internal class CombinedConfig(FastDataConfig fastDataConfig, CSharpGeneratorConfig cSharpGeneratorConfig)
{
    internal FastDataConfig FastDataConfig { get; } = fastDataConfig;
    internal CSharpGeneratorConfig CSharpGeneratorConfig { get; } = cSharpGeneratorConfig;
}