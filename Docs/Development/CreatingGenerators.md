# Creating Generators

Generators turn FastData structure contexts into source code for a target language. A generator is responsible for rendering the selected lookup structure, required helper functions, constants, values, early exits, and public API methods in a form that compiles for that language.

The existing C#, C++, and Rust generators are template-based and use `FastData.Generator.Template` with T4 templates. New generators should follow the same model unless there is a concrete reason not to.

## Naming

Generators must be named `<Lang>CodeGenerator`. The naming scheme is assumed by the templated generator infrastructure to derive the language name and locate templates.

Examples:

- `CSharpCodeGenerator`
- `CPlusPlusCodeGenerator`
- `RustCodeGenerator`

The companion config type should use the same prefix, such as `CSharpCodeGeneratorConfig`.

## Output Rules

- Generators should be able to run multiple times in the same output context.
- Each run should output only the selected structure and structure-specific data.
- Shared helpers, object declarations, imports, utility functions, and runtime support code must be emitted only once.
- Avoid hidden global state in generated code unless it is intentionally part of the target language pattern.
- Keep generated identifiers deterministic so repeated generation produces stable diffs.
- Prefer small target-language helpers over duplicating complex logic in every template.

## Configuration

Generator output should be configurable by users. The `FastData.Generator.Template` package helps build templatable generators using the T4 template engine.

Expose generator-wide placeholders through a language-specific config object instead of forcing users to edit every template. For example, in C# the user might want a static class instead of an instance class. They should set `ClassType = Static` rather than editing each template manually.

Common generator config options include:

- generated type or module name.
- namespace or package/module path.
- visibility/export mode.
- static versus instance API shape when the target language supports both.
- branch style choices, such as switch versus if/else, when both are valid.
- language-specific feature flags.

Templates receive the shared `GeneratorConfig`, the language-specific config, the selected structure `Context`, the simplified template `Data`, the `TypeMap`, and the key/value `Model`. Prefer these placeholders over ad-hoc template state.

## Templates

Template-based generators should derive from `TemplatedCodeGenerator` and implement `GenerateTemplated<TKey, TValue>()`.

Each structure template is selected by `genCfg.StructureName + ".tt"`. Template directories are resolved from the generator name, so a generator named `<Lang>CodeGenerator` should place templates under `Templates/<Lang>/` in its output layout.

Use the template data DTOs exposed by `FastData.Generator.Template.TemplateData` when available. They are intentionally simpler than the raw context objects and avoid exposing spans or generic runtime-only details to templates.

## Supporting Core Features

The FastData engine supports several core features that generator developers can use. To support them, the target generator must render the required expressions and helper functions correctly.

### Early Exits

Render early-exit expressions at the top of public API methods, before the main lookup structure runs.

Early-exit conditions mean "reject this key". Generated code must therefore emit the target-language equivalent of:

```csharp
if (condition)
    return false;
```

For lookup methods that return a value through an out parameter or result object, preserve the target language's existing miss behavior while returning the miss result.

Generators should support the expression shapes produced by the FastData expression pipeline, including comparisons, boolean operators, arithmetic, bitwise operators, constants, locals, helper calls, and assignments introduced by allocation transforms. When a target language cannot support a generated shape, fail clearly during generation rather than emitting invalid source.

### Helper Functions

Render only the helper functions required by the current `GeneratorConfig`. Helper usage is discovered from the generated expressions and structure needs.

String-oriented helpers commonly include:

- `Length`
- `UnitAt`
- `UnitAtAsciiLower`
- `EqualsAt`
- `EqualsAtAsciiLower`
- `IsAsciiOnly`

Helpers must match the semantic contract documented in `Docs/Architecture/EarlyExits.md`, especially around addressable string units, negative offsets, and ASCII-only case folding.

### Case Sensitivity

Provide an indirection for string comparison and switch between case-sensitive and case-insensitive matching depending on `Config.IgnoreCase`.

Case-insensitive matching is ordinal ignore-case. ASCII-only helper paths must not be used as general Unicode case folding. If a target language has no safe equivalent for a specific case-insensitive structure or branch style, reject that configuration explicitly.

### Key And Value Types

Use the provided `TypeMap` for target-language type names and literal handling. Do not infer type names independently in each template.

Respect the generator encoding selected by the language generator. Existing generators use UTF-16 code units for C# and UTF-8 bytes for C++ and Rust.

### Required Functions

The data structure selection logic validates required functions and chooses a compatible structure before template rendering.

Required function values are:

- `None`: no additional generated capabilities are required.
- `Membership`: generated code must support exact membership checks.
- `KeyValueLookup`: generated code must support key/value lookups.
- `Enumeration`: generated code must support lazy or native target-language iteration over exact keys and values.
- `DirectAccess`: generated code must support direct contiguous access to exact generated keys and values.

`Enumeration` means lazy or native target-language iteration over exact keys and values. Use idiomatic APIs for the language, such as `IEnumerable<T>` in C#, `keys()`/`values()` ranges with `begin()`/`end()` in C++, and iterators in Rust.

`DirectAccess` means contiguous access to exact generated keys and values. Use target-language contiguous views, such as `ReadOnlySpan<T>` in C#, C++17-compatible `keys()`/`values()` views with `data()`, `size()`, `operator[]`, `begin()`, and `end()`, or slices in Rust.

Reuse the arrays, entries, offsets, ranges, or encoded storage already required by the selected lookup structure. If a structure cannot enumerate exact data without extra occupancy information or lossy decoding, do not advertise the capability until the structure representation can support it correctly.

## Public API Shape

Every generated structure should expose consistent public API methods for membership and lookup according to the generator's language conventions.

Public API methods should:

- run early exits first.
- execute the selected structure lookup.
- perform final equality checks when the structure can produce candidates or hash collisions.
- use the configured case-sensitivity behavior for string keys.
- return the same miss result for early-exit misses and structure misses.

## Validation

Validate unsupported combinations in the generator before rendering templates. For example, the C# generator rejects switch-based conditional generation when `IgnoreCase` is enabled because that combination is not supported.

Prefer explicit `InvalidOperationException` messages that explain the unsupported option and the alternative when one exists.

## Testing

Generator changes should be covered by the relevant generator test project and, when behavior affects real generated code, by the test harness runner.

### Test Harness

Every production generator should provide a matching test harness project, such as `FastData.Generator.CSharp.TestHarness`, `FastData.Generator.CPlusPlus.TestHarness`, or `FastData.Generator.Rust.TestHarness`. The harness is used by shared runner projects to generate real target-language output, compile or execute it, and validate behavior across common test vectors.

The harness also enables generator benchmarking. Benchmark runners can reuse the same bootstrap code to compare generated lookup performance across languages and structures. Without a harness, a generator can still render source in isolation, but it cannot participate fully in cross-language correctness testing or benchmark coverage.

Useful commands:

```powershell
dotnet test --project Src/FastData.Generator.Tests/FastData.Generator.Tests.csproj -c Debug
dotnet test --project Src/FastData.TestHarness.Runner/FastData.TestHarness.Runner.csproj -c Debug
```

For language-specific changes, run the affected language tests or harness where available.