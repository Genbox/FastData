using Genbox.FastData.Configs;

namespace Genbox.FastData.Abstracts;

public interface ICodeGenerator
{
    bool UseUTF16Encoding { get; }
    bool TryGenerate(GeneratorConfig genCfg, IContext context, out string? source);
}