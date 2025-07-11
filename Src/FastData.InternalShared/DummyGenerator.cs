using Genbox.FastData.Enums;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;
using Newtonsoft.Json;

namespace Genbox.FastData.InternalShared;

public readonly struct DummyGenerator : ICodeGenerator
{
    public GeneratorEncoding Encoding => GeneratorEncoding.Unknown;

    public string Generate<T>(ReadOnlySpan<T> data, GeneratorConfig<T> genCfg, IContext<T> context)
    {
        Combined<T> combined = new Combined<T>(data.ToArray(), context);
        return JsonConvert.SerializeObject(combined, new JsonSerializerSettings { Formatting = Formatting.Indented });
    }

    private sealed class Combined<T>(T[] data, IContext<T> context)
    {
        public T[] Data { get; } = data;
        public IContext<T> Context { get; } = context;
    }
}