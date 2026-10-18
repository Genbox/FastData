# FastData.Generator.Ir

Shared lowering, lookup contracts, and backend preparation for IR-based FastData generators.

## Features

- **Structure lowering:** arrays, binary and interpolation search, conditionals, hash tables, compressed structures, ranges, bitsets, Bloom filters, and key-length lookups. Frontend hashing and early-exit expressions are translated into IR.
- **Lookup contracts:** `Find(key)` returns a storage slot or `-1`; membership-only lookups return any nonnegative hit marker. Values can reside in arrays or record fields.
- **Enumeration:** lazy key/value traversal that skips unused slots. Bloom, Elias–Fano, and RRR support membership only.
- **Key and text operations:** equality, ordering, hashing, ASCII case folding, and text access for UTF-8, ASCII, and UTF-16.
- **Builtins:** text operations, byte reads, multiply-high, and bit counting, with portable fallbacks for optional operations.
- **Backend preparation:** switch selection, integer promotion and wrapping, resolved calls, and shared names.
- **Metadata and execution:** named constants, lookup validation, and builtin support for the IR interpreter.

## Prepare for a backend

Inside `ICodeGenerator.Generate<TKey, TValue>`:

```csharp
using Genbox.FastData.Generator.Ir.Preparation;

IrBackendCapabilities capabilities = new IrBackendCapabilities(
    GeneratorEncoding.Utf16CodeUnits,
    IrBuiltins.Required,
    IrSwitchKinds.Integral | IrSwitchKinds.Character | IrSwitchKinds.String);

PreparedLookup prepared = IrGeneratorPreparation.Prepare<TKey, TValue>(config, context, capabilities);
```

Report `capabilities.Encoding` through `ICodeGenerator.Encoding`. Implement `IrBuiltins.Required` (text length and unit access) and advertise any additional native operations. Optional operations use IR fallbacks; `RuntimeFallbacks` also makes fallbacks available alongside native implementations.

Emit `prepared.Functions`, `Data` and `Records`, using their prepared names. Exported data carries its public name and `Exported` flag. `prepared.Api` supplies lookup and enumeration expressions; `prepared.RequiredBuiltins` identifies runtime helpers to implement.

Set `IntegerPromotionWidth = 32` for targets that promote narrow arithmetic. Set `ExplicitWrappingArithmetic = true` for targets such as checked C# or Rust. Preparation handles narrowing and wrapping; `PreparedWrappingArithmetic.Type` gives the result width.

Shared emission helpers keep backends focused on target syntax:

- `IrSourceEmitter`: expression and function traversal, variable bindings, exported declarations, and aggregate storage dependencies.
- `CStyleSourceEmitter`: common C-family operators, statements, and blocks.
- `LiteralFormatter`: packed literals, buffered string decoding, and references to prepared aggregate storage.
- `StatementWriter`: statement traversal through syntax hooks.
- `SourceWriter`: indentation and initializer layout.
- `CStyleOperators`: binary operator symbols.

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

Declare storage, helpers, and exports before calling `Build` once. Prepare a directly built lookup with `IrGeneratorPreparation.Prepare(lookup, capabilities)`, using matching encoding and builtin capabilities.

String queries must be non-null; stored floating-point keys must be finite. Floating-point searches reject NaN and treat signed zeros equally.

For enumerable lookups, use `lookup.Contract.Enumeration.EnumerateKeys(interpreter)` and `EnumerateValues(interpreter)`. The original program remains available as `prepared.Lookup.Program` for reference execution.