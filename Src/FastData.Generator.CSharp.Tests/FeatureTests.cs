using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Genbox.FastData.Configs;

namespace Genbox.FastData.Generator.CSharp.Tests;

public class FeatureTests
{
    // [Fact]
    // public void Do()
    // {
    //     FastDataConfig config = new FastDataConfig("MyData", ["a", "aaa", "aaaaa", "aaaaaaa"]);
    //     config.StorageMode = StorageMode.Indexed;
    //     string res = JsonSerializer.Serialize(config, GetOptions());
    // }

    [Theory, MemberData(nameof(GetInputs))]
    public void GenerateFeature(string inputFile)
    {
        string input = File.ReadAllText(inputFile);

        FastDataConfig? config = JsonSerializer.Deserialize<FastDataConfig>(input, GetOptions());
        Assert.NotNull(config);

        string source = FastDataGenerator.Generate(config, new CSharpCodeGenerator(new CSharpGeneratorConfig()));
        File.WriteAllText($@"..\..\..\Generated\Features\{Path.GetFileNameWithoutExtension(inputFile)}.output", source);
    }

    public static TheoryData<string> GetInputs()
    {
        TheoryData<string> data = new TheoryData<string>();
        data.AddRange(Directory.GetFiles(@"..\..\..\Generated\Features\", "*.input"));
        return data;
    }

    private static JsonSerializerOptions GetOptions()
    {
        return new JsonSerializerOptions
        {
            Converters = { new TypeNameConverter() },
            WriteIndented = true
        };
    }

    private sealed class TypeNameConverter : JsonConverter<object>
    {
        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            var typeInfo = new JsonObject
            {
                ["$type"] = value.GetType().Name,
                ["$value"] = JsonSerializer.SerializeToNode(value, value.GetType(), options)
            };
            typeInfo.WriteTo(writer);
        }

        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using JsonDocument doc = JsonDocument.ParseValue(ref reader);
            JsonElement root = doc.RootElement;

            if (root.TryGetProperty("$type", out JsonElement typeElement) && root.TryGetProperty("$value", out JsonElement valueElement))
            {
                string? typeName = typeElement.GetString();
                return JsonSerializer.Deserialize(valueElement.GetRawText(), _typeMap[typeName], options);
            }

            throw new JsonException("Invalid type information in JSON.");
        }

        private static readonly Dictionary<string, Type> _typeMap = new Dictionary<string, Type>
        {
            ["Int32"] = typeof(int),
            ["String"] = typeof(string),
        };
    }
}