using Genbox.FastData.Enums;
using JetBrains.Annotations;

namespace Genbox.FastData.Generator.Ir.CSharp;

/// <summary>Configures C# source generation through the portable IR.</summary>
[PublicAPI]
public sealed class CSharpIrCodeGeneratorConfig(string className) : IrCodeGeneratorConfig(className, GeneratorEncoding.Utf16CodeUnits)
{
    /// <summary>The namespace for the generated type, or null for the global namespace.</summary>
    public string? Namespace { get; set; }
}