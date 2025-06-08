namespace Genbox.FastData.Generators.Abstracts;

public interface ICodeGenerator
{
    bool UseUTF16Encoding { get; }
    bool TryGenerate<T>(GeneratorConfig<T> genCfg, IContext context, out string? source);
}