using Genbox.FastData.Configs;

namespace Genbox.FastData.Abstracts;

public interface IGenerator
{
    bool TryGenerate(GeneratorConfig genCfg, IContext context, out string? source);
}