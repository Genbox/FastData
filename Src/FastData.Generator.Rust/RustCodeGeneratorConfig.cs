using Genbox.FastData.Generator.Rust.Enums;
using JetBrains.Annotations;

namespace Genbox.FastData.Generator.Rust;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed class RustCodeGeneratorConfig(string className)
{
    public string ClassName { get; set; } = className;
    public RustOptions GeneratorOptions { get; set; }
}