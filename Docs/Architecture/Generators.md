# Generators

Language generators are the boundary between the language-neutral FastData core and target-language source code. The core pipeline selects a structure, prepares a context, builds a generator config, and then hands both to an `ICodeGenerator` implementation.

## Template Generation

Language generators derive from `TemplatedCodeGenerator`. The base class creates a common variable set for templates:

* `Model`: Key/value type metadata.
* `Context`: The raw structure context.
* `TypeMap`: Target-language type and literal formatting.
* `GeneratorConfig`: Numeric or string generator configuration.
* `Data`: Template-friendly DTO derived from the context.

Each language generator adds its own config object and renders `Templates/<Language>/<StructureName>.tt`. Shared includes in each language folder define headers, helper functions, metadata constants, and footers.

The template layer is responsible for target-language details:

* type names and literals through `TypeMap`.
* public API shape and configured class/module options.
* helper function emission.
* early-exit expression rendering.
* final equality checks and miss handling.
* target-language restrictions, such as unsupported branch styles.

Structure contexts are deliberately small and serializable into template-friendly DTOs. `TemplatedCodeGenerator` converts many raw contexts into `TemplateData` objects so templates do not need to understand spans, generics, or internal analysis types.

Generator authors should keep target-specific decisions in the generator package and keep the core pipeline language-neutral. See [CreatingGenerators.md](../Development/CreatingGenerators.md) for generator authoring rules.