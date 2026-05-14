using Genbox.FastData.Generator.Rust.Enums;
using JetBrains.Annotations;

namespace Genbox.FastData.Generator.Rust;

/// <summary>Configures Rust source generation.</summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed class RustCodeGeneratorConfig(string className)
{
    /// <summary>Gets or sets the generated Rust type name.</summary>
    public string ClassName { get; set; } = className;

    /// <summary>Gets or sets Rust-specific generator options.</summary>
    public RustOptions GeneratorOptions { get; set; }
}