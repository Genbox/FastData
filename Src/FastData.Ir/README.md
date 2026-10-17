# FastData IR language

A small, portable, intermediate representation language. Used to represent string hash/equality functions, data structures and perhaps other things in the future.

## Features

- Supports bool, signed and unsigned 8/16/32/64-bit integers, 32/64-bit floating point, ASCII and UTF-16 characters, strings, arrays, and records.
- Supports integer arithmetic, bitwise operations and shifts, comparisons, short-circuit logic, numeric conversions, floating-point bit casts, array indexing, record fields, and function calls.
- Supports control flow via parameters, mutable locals, assignments, conditional branches, loops, breaks, and returns.

## Type system

Operations use explicit types to keep behavior portable across backends. Assignments, return values, and call arguments must exactly match their declared types. Conversions are explicit; arithmetic does not promote narrow integers.

### Booleans

- `Bool` supports equality and short-circuit `AndAlso`/`OrElse`.
- Branch and loop conditions must be `Bool`.

### Integers

- Signed and unsigned integers have widths of 8, 16, 32, or 64 bits.
- Addition, subtraction, multiplication, and bitwise AND/OR/XOR require matching integral types and return that same type.
- Unsigned arithmetic wraps at the declared width. Signed arithmetic results must be representable.
- Division and modulo require matching unsigned types and a nonzero divisor.
- Shifts take an unsigned value and an `I32` count, returning the value's type. Counts must be from zero through width minus one; right shifts fill with zeros.
- Equality and ordered comparisons require matching types and return `Bool`.
- Integer conversions truncate to the target width, sign-extend signed sources when widening, and zero-extend unsigned sources. The target signedness determines how the resulting bits are interpreted.

### Floating point

- `F32` and `F64` preserve exact bits, including signed zero, infinities, and NaN payloads.
- Comparisons require matching types and follow IEEE semantics: signed zeros compare equal; with a NaN operand, only `NotEqual` returns true.
- Bit casts convert between a floating-point type and a signed or unsigned integer of the same width, preserving every bit.
- Floating-point arithmetic and numeric conversions involving floating point are unsupported.

### Characters

- `Char<Ascii>` represents values 0–127; `Char<Utf16CodeUnit>` represents values 0–65535, including individual surrogates.
- Character kinds are distinct types. Equality and ordering compare matching kinds by code-unit value.
- Characters convert explicitly to and from their corresponding integer type: `U8` for ASCII and `U16` for UTF-16. Values converted to characters must fit the character range.
- Character arithmetic uses an explicit conversion to the integer unit type.

### Strings

- Each program selects UTF-8 or UTF-16 for all strings. UTF-8 requires well-formed text; UTF-16 preserves unpaired surrogates.
- Equality compares text exactly; ordering is lexicographic by encoded code units. Core comparisons are case-sensitive and culture-independent.
- Strings support typed null literals and equality checks against null. Ordered comparisons require non-null strings.
- Runtime length and code-unit access are external operations supplied by libraries such as `FastData.Generator.Ir`.

### Arrays and records

- Arrays have one exact element type and use `I32` indices. Indexing returns the element type.
- Boolean, numeric, and character arrays use packed literals. String arrays use packed string storage; nested arrays and records use element literals. Arrays and records can nest.
- Record types are identified by their declaration ID. Two declarations remain distinct even when their fields have identical names and types.
- Field access requires a receiver of the declaring record type and returns the field's declared type.
- Arrays and records are immutable values.
- Arrays and records support typed nulls. Equality requires at least one operand to be a null literal.

## Semantics

- Program validation checks declarations, types, bindings, literal representations, definite assignment, and return paths.
- Functions have a declared result type and cannot fall through without returning; a nonterminating path is permitted.
- Locals and parameters can be reassigned; elements and fields are read-only.

Producers ensure valid bounds, non-null dereferences, nonzero integral divisors, representable signed arithmetic results, valid shift counts, and character conversions within range. The interpreter diagnoses violations; generated code can rely on these preconditions.

## Example

```csharp
using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Execution;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using static Genbox.FastData.Ir.Building.IrFactory;

IrProgramBuilder builder = new IrProgramBuilder();
IrFunctionBuilder square = builder.Function("Square", IrType.U64);
IrVariableId x = square.Parameter("x", IrType.U64);
square.Define(Block(Return(Multiply(Read(x), Read(x)))));
IrProgram program = builder.Build();
IrLiteral result = new IrInterpreter(program).Invoke(square.Id, new IrU64Literal(3));
// result is an IrU64Literal with Value == 9.
```