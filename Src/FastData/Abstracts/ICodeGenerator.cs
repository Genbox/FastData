using Genbox.FastData.Configs;

namespace Genbox.FastData.Abstracts;

public interface ICodeGenerator
{
    bool UseUTF16Encoding { get; }
    bool TryGenerate<T>(GeneratorConfig<T> genCfg, IContext context, out string? source);
}