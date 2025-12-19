# Repository Guidelines

## Project Structure & Module Organization
- `Src/` holds all production code. Core library lives in `Src/FastData`, generators in `Src/FastData.Generator*`, CLI in `Src/FastData.Cli`, and source generator in `Src/FastData.SourceGenerator`.
- Tests live in `Src/*Tests` (e.g., `Src/FastData.Tests`, `Src/FastData.Generator.CSharp.Tests`). Benchmarks are under `Src/*Benchmarks`.
- Shared test utilities are in `Src/FastData.InternalShared`. Examples are in `Src/FastData.Examples` and `Src/FastData.SourceGenerator.Examples`.
- Documentation is under `Docs/`, and build/publish scripts are in `Scripts/`.

## Build, Test, and Development Commands
- `dotnet build FastData.sln -c Debug` builds the full solution.
- `pwsh Scripts/Build.ps1` runs the scripted build (Debug by default).
- `dotnet test FastData.sln -c Debug` runs all test projects.
- `dotnet run -c Release --project Src/FastData.Benchmarks/FastData.Benchmarks.csproj` runs BenchmarkDotNet benchmarks.

## Coding Style & Naming Conventions
- Follow `.editorconfig`: C# uses 4-space indentation and file-scoped namespaces.
- Prefer explicit types over `var` (see `csharp_style_var_* = false`).
- Avoid adding trailing newlines; trim trailing whitespace on save.

## Testing Guidelines
- Tests use xUnit v3 (via `Microsoft.NET.Test.Sdk`).
- Snapshot-like expectations using Verify live under `Src/*Tests/Verify` as `.verified.txt` files.
- Add tests alongside the relevant `*Tests` project and keep naming aligned with existing patterns (e.g., `FeatureTests`, `VectorTests`).

## Local Configuration
- `Locals/` contains optional, machine-specific MSBuild overrides. Do not commit secrets or environment-specific paths.

## Others
- Don't touch public API files PublicAPI.Shipped.txt and PublicAPI.Unshipped.txt; they are auto-generated.