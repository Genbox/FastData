using Genbox.FastData.Models;

namespace Genbox.FastData.Abstracts;

public interface IGenerator
{
    string Generate(GeneratorConfig genCfg, FastDataConfig fastCfg, IContext context);
}