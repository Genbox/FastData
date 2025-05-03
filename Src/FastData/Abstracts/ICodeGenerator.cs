using Genbox.FastData.Configs;

namespace Genbox.FastData.Abstracts;

public interface ICodeGenerator
{
    bool TryGenerate(GeneratorConfig genCfg, IContext context, out string? source);
}