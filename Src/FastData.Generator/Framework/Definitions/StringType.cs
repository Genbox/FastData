using Genbox.FastData.Enums;

namespace Genbox.FastData.Generator.Framework.Definitions;

public readonly record struct StringType
{
    public StringType(GeneratorEncoding encoding, string typeName, Func<string, string>? print = null)
    {
        Encoding = encoding;
        StringTypeDef = new StringTypeDef(typeName, print);
    }

    internal GeneratorEncoding Encoding { get; }
    internal StringTypeDef StringTypeDef { get; }
}