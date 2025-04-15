using Genbox.FastData.Configs;

namespace Genbox.FastData.Abstracts;

public interface IGenerator
{
    string Generate(GeneratorConfig genCfg, IContext context);
}