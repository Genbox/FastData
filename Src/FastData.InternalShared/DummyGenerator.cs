using Genbox.FastData.Enums;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;
using Newtonsoft.Json;

namespace Genbox.FastData.InternalShared;

public readonly struct DummyGenerator : ICodeGenerator
{
    public GeneratorEncoding Encoding => GeneratorEncoding.Unknown;

    public string Generate<T>(GeneratorConfig<T> genCfg, IContext<T> context)
    {
        return JsonConvert.SerializeObject(context, new JsonSerializerSettings { Formatting = Formatting.Indented });
    }
}