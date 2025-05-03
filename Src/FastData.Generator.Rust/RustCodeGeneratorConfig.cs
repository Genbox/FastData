using Genbox.FastData.Generator.Rust.Enums;

namespace Genbox.FastData.Generator.Rust;

public sealed class RustCodeGeneratorConfig(string className)
{
    public string ClassName { get; set; } = className;
    public RustOptions GeneratorOptions { get; set; }
}