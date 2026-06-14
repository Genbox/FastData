# Project Structure

This repository keeps production code under `Src/`, documentation under `Docs/`, build scripts under `Scripts/`, and local machine overrides under `Locals/`.

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

## Documentation

* `Docs/Index.md`: High-level overview of FastData and the generation pipeline.
* `Docs/Architecture/`: Architecture docs, data-structure docs, and early-exit docs.
* `Docs/Development/`: Development-oriented documentation for contributors.
* `Docs/Diagrams/`: Architecture and structure diagrams.
* `Docs/Optimizations.md`: Optimization notes and links to detailed optimization docs.

## Supporting Folders

* `Imports/`: Shared project imports used by repository projects.
* `Locals/`: Machine-local MSBuild overrides. Do not commit secrets or environment-specific values.
* `.github/`: Issue templates and CI workflows.