# Architecture

FastData is split into a small core pipeline and several frontends that feed data into it.

## Projects

* `Src/FastData`: Core API, data analysis, structure selection, early exits, and structure contexts.
* `Src/FastData.Generator`: Shared language abstractions such as type maps and expression rendering.
* `Src/FastData.Generator.Template`: T4 template rendering and template data models.
* `Src/FastData.Generator.CSharp`: C# language definition, expression compiler, config, and templates.
* `Src/FastData.Generator.CPlusPlus`: C++ language definition, expression compiler, config, and templates.
* `Src/FastData.Generator.Rust`: Rust language definition, expression compiler, config, and templates.
* `Src/FastData.Cli`: Command-line frontend for file-based generation.
* `Src/FastData.SourceGenerator`: Roslyn incremental source generator frontend.
* `Src/FastData.SourceGenerator.Attributes`: Assembly-level attribute API consumed by the source generator.
* `Src/*Tests`: xUnit tests, snapshot outputs, and source generator verification.
* `Src/*Benchmarks`: BenchmarkDotNet projects and generated benchmark fixtures.

## Core Flow

`FastDataGenerator` is the central entry point. It has separate internal paths for numeric and string keys because they need different validation, analysis, early exits, and structure-selection rules.

Numeric flow:

1. Validate key type and reject `NaN` or infinity for floating-point keys.
2. Deduplicate keys and preserve or sort order according to config.
3. Compute `NumericKeyProperties` with ranges, density, bit masks, min/max, and zero presence.
4. Select a structure through `NumericStructures<TKey>` unless a structure override is supplied.
5. Build hash data for hash-based structures.
6. Build numeric early exits.
7. Create a numeric generator config and structure context.
8. Render source through the selected language generator.

String flow:

1. Reject null and empty string keys.
2. Deduplicate with ordinal or ordinal-ignore-case comparison.
3. Compute `StringKeyProperties` with length data, character data, and optional prefix/suffix data.
4. Reject non-ASCII data for generators that expose ASCII-byte APIs.
5. Select a structure through `StringStructures` unless a structure override is supplied.
6. Optionally benchmark string hash candidates for hash-based structures.
7. Trim common prefix/suffix data when enabled.
8. Build string early exits and transform expressions.
9. Create a string generator config and structure context.
10. Render source through the selected language generator.

## Template Generation

Language generators derive from `TemplatedCodeGenerator`. The base class creates a common variable set for templates:

* `Model`: Key/value type metadata.
* `Context`: The raw structure context.
* `TypeMap`: Target-language type and literal formatting.
* `GeneratorConfig`: Numeric or string generator configuration.
* `Data`: Template-friendly DTO derived from the context.

Each language generator adds its own config object and renders `Templates/<Language>/<StructureName>.tt`. Shared includes in each language folder define headers, helper functions, metadata constants, and footers.

## User-Facing Frontends

The CLI reads newline-delimited UTF-8 keys, parses them according to `--key-type`, creates a language generator, and writes generated source to standard output or `--output-file`.

The source generator scans assembly-level `FastDataAttribute<TKey>` and `FastDataKeyValueAttribute<TKey,TValue>` attributes. It converts attribute constants to runtime arrays, creates a C# generator, invokes `FastDataGenerator`, and adds `<ClassName>.g.cs` to the compilation.

The library API is the lowest-level user-facing entry point. Callers provide keys, a data config, and an `ICodeGenerator` implementation.