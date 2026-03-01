# Repository Guidelines

## Project Structure & Module Organization
- `Src/` holds all production code.
- Core library: `Src/FastData`.
- Generators: `Src/FastData.Generator*` (CSharp, CPlusPlus, Rust, Template).
- CLI: `Src/FastData.Cli`.
- Source generator: `Src/FastData.SourceGenerator` and attributes in `Src/FastData.SourceGenerator.Attributes`.
- Tests live under `Src/*Tests` (xUnit v3).
- Benchmarks live under `Src/*Benchmarks` (BenchmarkDotNet).
- Examples: `Src/FastData.Examples` and `Src/FastData.SourceGenerator.Examples`.
- Shared test utilities: `Src/FastData.InternalShared`.
- Test harnesses: `Src/*TestHarness*` and `Src/FastData.TestHarness.Runner`.
- Console/demo apps live under `Src/*Console` and `Src/*Demo`.
- Docs live under `Docs/`.
- Build/publish scripts live under `Scripts/`.
- Local machine overrides live under `Locals/` (do not commit secrets).

## Documentation & Examples
- Update docs in `Docs/` when behavior or flags change.
- Example projects live in `Src/FastData.Examples` and `Src/FastData.SourceGenerator.Examples`.
- Keep example code minimal and aligned with current APIs.

## Build, Lint, and Test Commands
- Build full solution: `dotnet build FastData.slnx -c Debug`.
- Build Release: `dotnet build FastData.slnx -c Release`.
- Scripted build: `pwsh Scripts/Build.ps1`.
- Test full solution: `dotnet test FastData.slnx -c Debug`.
- Test a single project: `dotnet test Src/FastData.Tests/FastData.Tests.csproj -c Debug`.
- Test project list:
  - `Src/FastData.Tests/FastData.Tests.csproj`
  - `Src/FastData.Cli.Tests/FastData.Cli.Tests.csproj`
  - `Src/FastData.Generator.Tests/FastData.Generator.Tests.csproj`
  - `Src/FastData.SourceGenerator.Tests/FastData.SourceGenerator.Tests.csproj`
- Run a single test by name:
  - `dotnet test Src/FastData.Tests/FastData.Tests.csproj -c Debug --filter "FullyQualifiedName~Namespace.ClassName.TestName"`
  - `dotnet test Src/FastData.Tests/FastData.Tests.csproj -c Debug --filter "DisplayName~some substring"`
- Run a single test class: `dotnet test Src/FastData.Tests/FastData.Tests.csproj -c Debug --filter "FullyQualifiedName~Namespace.ClassName"`.
- Optional analyzers (lint-like): `dotnet build FastData.slnx -c Debug -p:RunAnalyzersDuringBuild=true`.
- Run benchmarks: `dotnet run -c Release --project Src/FastData.Benchmarks/FastData.Benchmarks.csproj`.
- Generator benchmark harness:
  - `dotnet run -c Release --project Src/FastData.BenchmarkHarness.Runner/FastData.BenchmarkHarness.Runner.csproj`
  - `dotnet run -c Release --project Src/FastData.BenchmarkHarness.Runner/FastData.BenchmarkHarness.Runner.csproj CSharp`
  - `dotnet run -c Release --project Src/FastData.BenchmarkHarness.Runner/FastData.BenchmarkHarness.Runner.csproj CPlusPlus`
  - `dotnet run -c Release --project Src/FastData.BenchmarkHarness.Runner/FastData.BenchmarkHarness.Runner.csproj Rust`
- Run a specific benchmark (BenchmarkDotNet): `dotnet run -c Release --project Src/FastData.Benchmarks/FastData.Benchmarks.csproj -- --filter "*Hash*"`.

## Common Test Examples
- CLI tests: `dotnet test Src/FastData.Cli.Tests/FastData.Cli.Tests.csproj -c Debug`.
- Generator tests: `dotnet test Src/FastData.Generator.Tests/FastData.Generator.Tests.csproj -c Debug`.
- Source generator tests: `dotnet test Src/FastData.SourceGenerator.Tests/FastData.SourceGenerator.Tests.csproj -c Debug`.
- Single test in CLI tests: `dotnet test Src/FastData.Cli.Tests/FastData.Cli.Tests.csproj -c Debug --filter "FullyQualifiedName~Namespace.ClassName.TestName"`.
- Single test in generator tests: `dotnet test Src/FastData.Generator.Tests/FastData.Generator.Tests.csproj -c Debug --filter "DisplayName~some substring"`.

## Language & Project Settings
- C# `LangVersion` is `latest` with `Features` set to `strict`.
- Nullable reference types are enabled; keep annotations accurate.
- Implicit usings are enabled, but prefer explicit using directives for clarity.
- Overflow checking is enabled in Debug (`CheckForOverflowUnderflow=true`).
- NuGet uses lock files (`packages.lock.json`) and central package management.
- Release builds generate XML docs; Debug does not.

## Package Management
- Central package management lives in `Src/Directory.Packages.props`.
- Analyzer packages are versioned in `Src/Directory.Packages.Analyzers.props`.

## Coding Style & Conventions
- Respect `.editorconfig` (UTF-8, trim trailing whitespace, no final newline).
- Indentation: 4 spaces for C#; 2 spaces for JSON/XML/JS/CSS.
- Max line length is large; still wrap for readability and diff friendliness.
- Use file-scoped namespaces.
- Use explicit accessibility modifiers (per Roslynator settings).
- Prefer explicit types over `var` (see `csharp_style_var_* = false`).
- Keep line length reasonable even though max is large; wrap for readability.
- Use explicit using directives even with implicit usings enabled.
- Using order: `System.*` first, then `Genbox.FastData.*`, then other externals (e.g., `Microsoft.*`).
- Modifier order: `public, private, protected, internal, new, sealed, static, unsafe, override, extern, async, virtual, abstract, volatile, readonly`.
- Preserve single-line blocks when already single-line (`csharp_preserve_single_line_blocks = true`).
- No space after casts (`csharp_space_after_cast = false`).
- Prefer explicit object creation types even when type is evident.
- Braces are required for multiline control blocks; keep single-line statements compact when already single-line.
- Use expression-bodied members for simple, single-expression methods/properties.
- Prefer guard clauses and early returns for input validation.
- Use `InvalidOperationException` for invalid state, `ArgumentException`/`ArgumentOutOfRangeException` for bad inputs.
- Avoid `null` for non-nullable references; honor nullable annotations (`?`, `!`).
- Use `ReadOnlyMemory<T>`/`ReadOnlySpan<T>` when working with large buffers.
- Keep allocations explicit and minimal in hot paths.
- Prefer `StringComparer.Ordinal`/`OrdinalIgnoreCase` and `StringComparison.Ordinal`/`OrdinalIgnoreCase`.
- Favor `ValueTuple` over `Tuple`.
- Prefer `CultureInfo.GetCultureInfo` over `new CultureInfo(string)`.

## Formatting Details
- XML/JSON/JS/CSS use 2-space indentation.
- C# uses 4-space indentation and no tabs.
- Avoid adding trailing whitespace and avoid final newlines.
- Keep blank lines minimal; follow existing spacing in the file.
- Keep existing single-line initializers on one line when already compact.

## Additional C# Preferences
- Keep object/collection initializer members on the same line when already short.
- Prefer expression-bodied local functions where they remain clear.
- Avoid implicit object creation; be explicit with `new TypeName(...)`.
- Keep attribute placement and formatting consistent with existing files.
- Avoid excessive blank lines around fields and members.
- Keep embedded statements on their own line unless already single-line.

## Naming Conventions
- Types and public members: `PascalCase`.
- Private fields: `_camelCase`.
- Locals and parameters: `camelCase`.
- Interfaces: `IName`.
- Enum values: `PascalCase`.

## Error Handling & Logging
- Prefer explicit validation with clear exception messages.
- Log via `Microsoft.Extensions.Logging` where there is user-configurable behavior.
- Use `NullLoggerFactory.Instance` as a safe default when no factory is provided.
- Avoid swallowing exceptions; propagate unless a specific recovery path exists.

## Performance & Memory
- Favor spans/memory for large buffers to avoid copies.
- Keep allocations visible and intentional, especially in generators and analysis loops.
- Use stackalloc only when sizes are small and bounded.
- Avoid LINQ in hot paths when it obscures allocations.

## Testing Guidelines
- Tests use xUnit v3 (via `Microsoft.NET.Test.Sdk`).
- Snapshot-like expectations live under `Src/*Tests/Verify` as `.verified.txt` files.
- Verify files (`*.verified.*`, `*.received.*`) use UTF-8 BOM and LF; do not trim trailing whitespace.
- Keep test names and files aligned with existing patterns (e.g., `FeatureTests`, `VectorTests`).

## Generated Code & Artifacts
- Do not modify `PublicAPI.Shipped.txt` or `PublicAPI.Unshipped.txt` (auto-generated).
- Do not modify generated C# code-behind files that correspond to `.t4` templates (the `.t4` files themselves are fine to edit).
- Avoid editing generated outputs from tools or templates unless the change is made at the source and regenerated.

## Local Configuration
- `Locals/` contains machine-specific MSBuild overrides. Do not commit secrets or environment-specific paths.