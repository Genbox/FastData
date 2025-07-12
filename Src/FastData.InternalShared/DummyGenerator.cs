using Genbox.FastData.Enums;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;
using Newtonsoft.Json;

namespace Genbox.FastData.InternalShared;

public readonly struct DummyGenerator : ICodeGenerator
{
    public GeneratorEncoding Encoding => GeneratorEncoding.Unknown;

    public string Generate<TKey, TValue>(GeneratorConfig<TKey> genCfg, IContext<TValue> context)
    {
        return JsonConvert.SerializeObject(context, new JsonSerializerSettings { Formatting = Formatting.Indented });
    }
}