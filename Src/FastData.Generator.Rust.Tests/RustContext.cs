using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Generator.Rust.Shared;

namespace Genbox.FastData.Generator.Rust.Tests;

[SuppressMessage("Design", "CA1034:Nested types should not be visible")]
[SuppressMessage("Maintainability", "CA1515:Consider making public types internal")]
public sealed class RustContext
{
    public RustCompiler Compiler { get; } = new RustCompiler(false, Path.Combine(Path.GetTempPath(), "FastData", "Rust"));
}