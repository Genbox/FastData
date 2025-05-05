using Genbox.FastData.Configs;

namespace Genbox.FastData.Abstracts;

public interface ICodeGenerator
{
    bool UseUTF16Encoding { get; }
    bool TryGenerate<T>(GeneratorConfig genCfg, IContext context, out string? source);
}