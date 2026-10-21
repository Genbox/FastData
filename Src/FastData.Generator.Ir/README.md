# FastData.Generator.Ir

Shared lowering, lookup contracts, and backend preparation for IR-based FastData generators.

## Features

- **Structure lowering:** arrays, binary and interpolation search, conditionals, hash tables, compressed structures, ranges, bitsets, Bloom filters, and key-length lookups. Frontend hashing and early-exit expressions are translated into IR.
- **Lookup contracts:** `Find(key)` returns a storage slot or `-1`; membership-only lookups return any nonnegative hit marker. Values can reside in arrays or record fields.
- **Enumeration:** lazy key/value traversal that skips unused slots. Bloom, Elias-Fano, and RRR support membership only.
- **Key and text operations:** equality, ordering, hashing, ASCII case folding, and text access for UTF-8, ASCII, and UTF-16.
- **Builtins:** text operations, byte reads, multiply-high, and bit counting, with portable fallbacks for optional operations.
- **Backend preparation:** storage planning, resolved calls, and shared names; emitters resolve arithmetic types and choose dispatch syntax.
- **Metadata and execution:** named constants, lookup validation, and builtin support for the IR interpreter.

## Lowering structure contexts

`ContextLowerer` owns the sequence: lower early exits, dispatch the structure context, export constants, and build the lookup. Each structure has a static `Lower` function in `DataStructures/`, taking the shared `LoweringContext` explicitly and returning its statement list. Structure algorithms and their private helpers stay together in their own files.

`LoweringContext` holds per-lookup builders and storage identities. `IntegerStorageBuilder` returns typed data IDs; the stored element type is available from `id.Type.ElementType`. Its unsigned overloads infer the full storage width from the input array, with nonnegative `int[]` values using `U32`. It shares array storage, optional stored-hash comparisons, and sparse hash-entry setup. The nongeneric `LookupEnumerationBuilder` derives key and value types from the builder bindings. `LookupEnumerationBuilder.AddCursor` handles cursor-to-slot conversion for both dense and sparse arrays; sparse structures supply an occupancy expression. Pure integral conversions live in `LoweringHelpers`.

`FrontendExpressions` translates early exits and hashes, sharing captured data across translator scopes. Operator and shift lowering use the translated operands' IR types directly. Lowering receives only the text encoding and emits builtin declarations independently of backend capabilities. `LoweredLookup` carries the completed core program and directly exposable storage into preparation.

## Prepare for a backend

Preparation owns aggregate discovery, storage names, dependency order, and rendering metadata. `PreparedFunction.Function` retains the original core `IrFunction`; preparation adds only generated names and call bindings. Emitters pair the original parameter and local declarations with generated variable names, without allocating prepared declarations for each variable. Every core expression exposes its result `Type`; emitters read operand types directly, without a preparation resolver or type hints. One callable table supplies generated names and native builtin choices, including aliases to portable fallback functions. Function preparation resolves the entire call table, including fallback aliases, before creating prepared functions. Every `PreparedFunction` requires explicit call bindings; functions with no core calls pass an empty list. Fallback functions are appended after the original functions. Fallback builders are created on demand, and their call remapping is an ordered list. Emitters render the original nodes using that information; preparation never rewrites a body. Semantic transformations belong in core IR before preparation.

`PreparedRecord.Record` retains the original record declaration alongside generated field names; no per-field declaration wrappers are created. Backend function formatting receives only the generated name, return type, rendered parameters, and body.

`DataPreparation` also uses the core `IrWalker`. It discovers aggregate literals in declarations, function bodies, and synthetic companion operands exposed through `IrExtensionExpression`. Packed storage is never expanded into individual expression nodes. Each emission plan owns its storage names and reference map while retaining original literal payloads. Function preparation transfers its completed function list without copying it. Data preparation uses a fresh, privately constructed walker per build and transfers its completed declaration list and reference map to the plan; the walker is not reused.

Derive each backend from `IrCodeGenerator`. Set `Encoding` on the generator config and override `SupportedBuiltins` to declare the operations implemented by the emitter. Implement `CreateEmitter(PreparedLookup lookup)` to construct the language renderer.

```csharp
public override IrBuiltinOperation SupportedBuiltins => IrBuiltins.Required;

protected override IrSourceEmitter CreateEmitter(PreparedLookup lookup) =>
    new MyEmitter(config, new EmissionContext(lookup));
```

Pass the config to the `IrCodeGenerator` base constructor. `IrCodeGeneratorConfig.Encoding` defaults to UTF-8; `CSharpIrCodeGeneratorConfig` defaults to UTF-16 code units. Users can set the property to select their encoding.

`IrCodeGenerator.Generate<TKey, TValue>` prepares the lookup, creates its emitter, and renders the result. Preparation is an implementation detail of the generator; callers obtain an emitter through `CreateEmitter<TKey, TValue>(config, context)` and can inspect its `Lookup`.

Implement `IrBuiltins.Required` (text length and unit access) and advertise any additional native operations through `SupportedBuiltins`. Preparation resolves builtin declarations against `SupportedBuiltins` for both frontend-generated and directly supplied programs. Supported operations remain builtin calls; unsupported optional operations become calls to prepared IR implementations. Those implementations may require additional native text primitives. The original validated program remains unchanged for reference execution.

Emit `prepared.EmissionPlan.Functions`, `prepared.EmissionPlan.Data`, and `prepared.Records`, using their prepared names. Exported data carries its public name and `Exported` flag. The retained lookup contract describes the available members and typed bindings; `prepared.RequiredBuiltins` identifies runtime helpers to implement.

`PreparedLookup` retains the original lookup contract and callable table, with `FunctionName(id)` resolving generated names and fallback aliases. Optional key/value slice storage is represented directly by data IDs. `FastDataProgram` derives its key and value types from validated bindings. `PublicApi` renders the fixed membership tests, slot access, and cursor expressions directly. `RenderedLookupApi` holds only the key type and search expressions; values and slices are rendered when their members are emitted. Index and field syntax is shared with core expression rendering through the backend access hooks. No temporary IR variables or expression trees are created for these API wrappers.

Rendering resolves arithmetic operand and result types from the original IR. Each language emitter handles native integer promotion, narrowing, and unsigned wrapping according to its own arithmetic rules.

Shared emission helpers keep backends focused on target syntax:

- `PreparedEmissionPlan`: complete dependency-ordered storage, fixed aggregate references, and prepared functions for one scope.
- `IrSourceEmitter`: fixed emission context, literal/variable/data references, and declaration assembly. Each language derives directly from this base.
- `LiteralFormatter`: shared literal dispatch, packed-array traversal, pooled string decoding, and fixed aggregate references. Type formatting and eight literal hooks cover signed/unsigned integers, float/double, characters, strings, nulls, and aggregates.
- `RenderedLookupApi`: source expressions for API and enumeration templates, used by the shared public API composition.
- `SourceWriter`: indentation, initializer layout, and source fragments without trailing line breaks.
- `CStyleOperators`: binary operator symbols.

`IrCodeGenerator` builds a complete `PreparedLookup.EmissionPlan` before creating the emitter; emitters render that plan without running preparation. Harness companion scopes create their own `PreparedEmissionPlan` before rendering. Planning discovers aggregates in declarations, function bodies, and operands exposed by `IrExtensionExpression.Children`; packed arrays retain their typed storage. The original `PreparedLookup.Data` remains indexed by IR data ID, while the plan has a separate dependency-ordered storage list.

`DataDeclarations(indent)` and `FunctionDeclarations(indent)` render the fixed plan directly into templates at the requested indentation. Expressions and declarations return strings. The base renders parameters, locals, and statements, then calls the backend's `Function(name, returnType, parameters, body)` raw-string template. Backends supply `Parameter` and `Local` syntax. Core and prepared bodies contain only the original `if` statements. The shared `WriteStatements` implementation renders original statements in source order and performs no switch detection. Each language emitter opts into its own control-flow optimization by overriding `WriteStatements`: C# and C++ recognize supported groups and emit switches; Rust emits match arms. They explicitly construct `SwitchDetectionVisitor` with the function's variable-name table. A successful visit publishes `VariableName`, `Type`, and the original label/result pairs, so emitters only choose syntax and render the cases. Each matched group is consumed once: the emitter renders a native construct or writes all original `if` statements when their type is unsupported. Unmatched statements go directly to `WriteStatement`. Languages without a switch can use the default block renderer without a switch-specific hook or configuration. There is no prepared switch node, dispatch prepass, or function-level dispatch dictionary. C# uses content comparisons for byte-array text, while C++ uses comparisons for all text encodings. The template chooses body indentation. Use `SourceWriter.Indent(source, indent)` for one fragment and `SourceWriter.JoinLines(fragments, indent)` for several; static imports keep templates concise. `SourceWriter` accepts an `initialIndent` for nested fragments. Backends compose these fragments with raw-string templates for their public APIs and file scaffolding. Construct the literal formatter in the backend and pass it directly to the `IrSourceEmitter` constructor. Bind aggregate references to `context.Plan.Reference`; Rust qualifies them with `Self::` and overrides `MemberReference` for function and data references. The base exposes type, literal, initializer, and element formatting without exposing the formatter instance.

The base renderer handles literals, references, field and index dispatch, logical operators, numeric comparisons, and resolved calls. Each backend supplies field/index syntax and renders casts, arithmetic, string comparisons, null checks, and native builtins. `IrExtensionExpression` exposes operands of synthetic companion expressions; these do not replace nodes in the lookup IR. Array initializer rendering accepts `IrArrayLiteralBase` directly. Function signatures stay explicit in `Function`, with declaration syntax in `Parameter` and `Local`. `EmitSource` supplies the complete language-specific lookup template; the non-virtual base `Emit()` writes it, invokes the configured append callback, and returns the combined source. Type formatting delegates to `LiteralFormatter.Type`; Rust shares lifetime-aware formatting through `RustTypes`. Emitters own their text helpers directly; literal formatters keep theirs private, so emission does not downcast `LiteralFormatter` to reach language helpers. Shared syntax is reused through helpers rather than another emitter base class.

Integer hooks receive a losslessly widened `long` or `ulong`, the original `IrTypeKind`, and whether the value is an initializer element. Backend switches keep casts, suffixes, and minimum-value syntax explicit. `float` and `double` stay separate to preserve bit patterns. Booleans use the shared `true`/`false` spelling, and arrays/records share a backend aggregate switch.

Packed arrays are enumerated directly from typed storage: no per-element IR nodes or boxed numeric values are created. Packed strings retain pooled decoding into spans. Floating-point bit extraction uses `BitConverter` bit APIs, with the repository compatibility helper for .NET Standard, avoiding temporary byte arrays.

`IrCodeGeneratorConfig` supplies the generated type name and encoding; C# extends it with a namespace.

The configured `GeneratorEncoding` determines emitted string storage, public key/value types, record fields, enumeration, and runtime operations:

| Encoding          | C#       | C++                             | Rust     |
|-------------------|----------|---------------------------------|----------|
| ASCII bytes       | `byte[]` | `std::span<const std::uint8_t>` | `&[u8]`  |
| UTF-8 bytes       | `byte[]` | `std::u8string_view`            | `&str`   |
| UTF-16 code units | `string` | `std::u16string_view`           | `&[u16]` |

`PreparedLookup.Encoding` snapshots the exact `GeneratorEncoding` during preparation; lookup and companion renderers read it directly from `EmissionContext.Lookup.Encoding`. `IrTextEncoding` continues to describe IR code-unit semantics, but must not select API types because it combines ASCII and UTF-8. Changing a generator config after preparation does not change an existing lookup's text representation. Each backend consolidates its text representation and operations in `CSharpText`, `CPlusPlusText`, or `RustText`. These helpers own text types, literals, comparisons, builtins, and generated text helpers; runtime classes handle numeric operations.

Internal C++ and Rust references remain optional to represent null storage entries. Callers supply keys in the configured representation. ASCII applies to all emitted string literals, including values and record fields; non-ASCII literals cannot be emitted under that setting. C++ ASCII literals have static byte-array backing storage, so their views remain valid after the lookup returns. C++ span comparisons compare contents, including lexicographic ordering and null handling. UTF-16 literals preserve individual code units, including unpaired surrogates. Comparisons use the encoded units, and byte-read offsets refer to the little-endian byte representation of those units. Generated C# byte reads target little-endian hosts and use direct unaligned reads. C# byte strings use content comparison and conditional dispatch because arrays cannot be switch labels. `Utf16Bytes` is not an IR code-unit encoding and remains unsupported.

Companion programs use `IrCodeGenerator.CreateEmitter` to obtain the lookup emitter, then call `CreateCompanion(plan, customExpression)` for an independent rendering context. Extension expressions expose their operands through `IrExtensionExpression.Children`; callbacks receive rendered operands. Named benchmark calls are harness-owned extension expressions and are rendered through this callback. `IrSourceEmitter` only handles core IR calls. The harness runner explicitly joins lookup source from `Emit()` with the result of `RenderTests` or `RenderBenchmark`. Each language supplies its harness scope through `Wrap`.

Use prepared declaration, function-call, and variable names. Reserve the `fdRecord`, `fdField`, `fdData`, `fdLiteral`, `fdFunction`, and `fdVariable` prefixes.

## Build and execute a lookup

This lookup maps integer key `7` to string value `"seven"`:

```csharp
using Genbox.FastData.Generator.Ir.Building;
using Genbox.FastData.Generator.Ir.Execution;
using Genbox.FastData.Generator.Ir.Model;
using Genbox.FastData.Ir.Execution;
using Genbox.FastData.Ir.Model;
using static Genbox.FastData.Ir.Building.IrFactory;

FastDataProgramBuilder builder = new FastDataProgramBuilder(IrType.I32);
IrDataId values = builder.Program.AddData("values", builder.Program.Text.PackStrings(["seven"]));
builder.Values = new LookupValues(values);
builder.ExportConstant("ItemCount", I32(1));

FastDataProgram lookup = builder.Build(Block(
    If(builder.Keys.Equal(Read(builder.KeyParameterId), I32(7)), Return(I32(0))),
    Return(I32(-1))));

IrInterpreter interpreter = new IrInterpreter(lookup.Program, IrBuiltinEvaluator.CreateHandler(lookup.Program));
IrI32Literal slot = (IrI32Literal)interpreter.Invoke(lookup.Contract.FindFunctionId, I32(7));
bool found = slot.Value >= 0; // Slot 0 holds "seven".
```

Declare storage, helpers, and exports before calling `Build` once. Pass a directly built lookup to `generator.CreateEmitter(lookup)`, using the generator's encoding and builtin support when constructing the program. The generator prepares the lookup internally; the returned emitter exposes it through `Lookup`.

String queries must be non-null; stored floating-point keys must be finite. Floating-point searches reject NaN and treat signed zeros equally.

For enumerable lookups, use `lookup.Contract.Enumeration.EnumerateKeys(interpreter)` and `EnumerateValues(interpreter)`. The original program remains available as `prepared.Lookup.Program` for reference execution.