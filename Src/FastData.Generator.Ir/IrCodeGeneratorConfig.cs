using Genbox.FastData.Enums;
using JetBrains.Annotations;

namespace Genbox.FastData.Generator.Ir;

/// <summary>Configures the generated type and text encoding for an IR backend.</summary>
[PublicAPI]
public class IrCodeGeneratorConfig(string className, GeneratorEncoding encoding = GeneratorEncoding.Utf8Bytes)
{
    /// <summary>The generated type name.</summary>
    public string ClassName { get; set; } = className;

    /// <summary>The string encoding model used by the generated lookup.</summary>
    public GeneratorEncoding Encoding { get; set; } = encoding;
}