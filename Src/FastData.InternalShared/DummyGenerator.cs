using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;
using Newtonsoft.Json;

namespace Genbox.FastData.InternalShared;

public sealed class DummyGenerator : ICodeGenerator
{
    public bool UseUTF16Encoding => true;

    public bool TryGenerate<T>(GeneratorConfig<T> genCfg, IContext context, out string? source)
    {
        source = JsonConvert.SerializeObject(context, new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        });
        return true;
    }
}