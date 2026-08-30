using Genbox.FastData.Enums;

namespace Genbox.FastData.Generator.Definitions;

/// <summary>Pairs a string encoding with its target-language type definition.</summary>
public readonly record struct StringType
{
    /// <summary>Initializes a new instance of the <see cref="StringType"/> structure.</summary>
    /// <param name="encoding">The string encoding used by the target language.</param>
    /// <param name="typeName">The target-language string type name.</param>
    /// <param name="print">The function used to format string literals.</param>
    public StringType(GeneratorEncoding encoding, string typeName, Func<string, string> print)
    {
        Encoding = encoding;
        StringTypeDef = new StringTypeDef(typeName, print);
    }

    internal GeneratorEncoding Encoding { get; }
    internal StringTypeDef StringTypeDef { get; }
}