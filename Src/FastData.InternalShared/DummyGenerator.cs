using System.Reflection;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;
using Newtonsoft.Json;

namespace Genbox.FastData.InternalShared;

public readonly struct DummyGenerator : ICodeGenerator
{
    public GeneratorEncoding Encoding => GeneratorEncoding.UTF16;

    public string Generate<TKey, TValue>(GeneratorConfig<TKey> genCfg, IContext context)
    {
        JsonSerializerSettings settings = new JsonSerializerSettings { Formatting = Formatting.Indented };
        settings.Converters.Add(new ReadOnlyMemoryJsonConverter());

        return JsonConvert.SerializeObject(context, settings);
    }

    // We need this converter since newtonsoft does not support ReadOnlyMemory
    private sealed class ReadOnlyMemoryJsonConverter : JsonConverter
    {
        private static readonly MethodInfo _writer = typeof(ReadOnlyMemoryJsonConverter).GetMethod(nameof(WriteJsonInternal), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)!;

        public override bool CanRead => false;

        public override bool CanConvert(Type objectType) => objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(ReadOnlyMemory<>);

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            Type elementType = value.GetType().GetGenericArguments()[0];
            _writer.MakeGenericMethod(elementType).Invoke(null, new[] { writer, value, serializer });
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            throw new NotSupportedException("ReadOnlyMemory deserialization is not supported.");
        }

        private static void WriteJsonInternal<T>(JsonWriter writer, ReadOnlyMemory<T> memory, JsonSerializer serializer)
        {
            writer.WriteStartArray();

            ReadOnlySpan<T> span = memory.Span;
            for (int i = 0; i < span.Length; i++)
                serializer.Serialize(writer, span[i]);

            writer.WriteEndArray();
        }
    }
}