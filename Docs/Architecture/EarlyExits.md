# Early Exits

Early exits are small generated checks that run before a lookup structure. Their job is to reject keys that cannot be present without paying the cost of hashing, searching, decoding a compressed structure, or scanning arrays.

An early exit is not the source of truth. It can only reject impossible keys. If a key passes every early exit, the generated code still performs the normal structure lookup.

## Main Types

`IEarlyExit` is the core contract:

```csharp
public interface IEarlyExit
{
    ulong KeyspaceSize { get; }
    Expression GetExpression(ParameterExpression key);
    bool IsWorseThan(IEarlyExit other);
}
```

`KeyspaceSize` estimates how much of the measured keyspace the check rejects. `GetExpression()` returns the expression-tree condition that means "reject this key". `IsWorseThan()` tells the optimizer when another early exit makes this one redundant.

Early exits are carried through the generator as `AnnotatedExpr` values. The annotation distinguishes executable early-exit conditions from local-variable allocations used by generated expressions.

## Configuration

`EarlyExitConfig` controls whether early exits are generated and how aggressively candidates are kept.

The default configuration uses:

- `MinItemCount = 3`
- `MaxCandidates = 4`
- `Optimize = true`
- `MinRejectionRatio = 0.05f`
- generated early exits disabled for `RangeStructure` and `SingleValueStructure`
- density limits for value bitmasks, length bitmaps, and unit-at bitmaps

There are two different kinds of early exits:

- Analysis-generated exits are optional candidates produced from observed key properties and filtered by `EarlyExitConfig`.
- Mandatory exits are required by a selected structure or hash implementation for correctness or safe indexing.

`EarlyExitConfig.Disabled` only disables generated candidates. Mandatory exits can still be added by structures because the structure may rely on them.

Analysis-generated early exit candidates are filtered by a rejection ratio threshold. The default is `EarlyExitConfig.MinRejectionRatio = 0.05`, meaning a candidate must reject at least 5% of the measured keyspace to be kept. Numeric candidates are measured against the analyzed numeric range. String candidates are measured against the observed length span, first-unit span, or last-unit span, depending on the exit.

Only the best candidates are kept. The default is `EarlyExitConfig.MaxCandidates = 4`, sorted by rejected keyspace size divided by estimated cost. Candidates can also be disabled globally, per structure, per early-exit type, or by type-specific density limits. The defaults only allow value bitmasks with density up to `0.25`, and length/character bitmaps with density up to `0.45`. Numeric analysis is skipped when the item count is less than or equal to `EarlyExitConfig.MinItemCount`; string analysis does not use that item-count gate.

After mandatory and analysis-generated exits are combined, FastData removes duplicate exits and, when expression optimization is enabled, removes weaker overlapping bounds. For example, `Length(input) < 3` is removed if `Length(input) < 5` is also present.

## Numeric Flow

Numeric generation starts in `FastDataGenerator` after input validation, deduplication, numeric analysis, and structure selection.

1. Numeric key analysis produces `NumericKeyProperties`, including observed min/max values, data ranges, density, range size, and bit masks.
2. A numeric structure is selected or created from the user override.
3. `NumericEarlyExits<TKey>.GetExits()` produces analysis-generated candidates.
4. `structure.GetMandatoryExits()` produces structure-required checks.
5. `EarlyExitPipeline.CombineAndDedup()` merges mandatory exits and analysis exits while preserving first-seen order.
6. If `EarlyExitConfig.Optimize` is true, `EarlyExitPipeline.Optimize<TKey>()` reduces and merges exits.
7. `EarlyExitPipeline.Annotate()` converts each `IEarlyExit` to an `AnnotatedExpr` over the input parameter named `key`.
8. If expression optimization is enabled, `EarlyExitPipeline.OptimizeExpressions()` simplifies the expression trees.
9. `NumericGeneratorConfig` carries the final annotated expressions to the language generator.

Numeric candidates come from `NumericEarlyExits<TKey>.ProduceCandidates()`. Candidates are filtered by rejection ratio, scored by rejection ratio divided by estimated cost, then capped by `MaxCandidates`.

### Numeric Early Exit Types

#### Value Less Than

`ValueLessThanEarlyExit<TKey>` rejects keys that are smaller than the minimum observed value. Input `4 9 42 99 123` can yield:

```csharp
if (value < 4)
    return false;
```

#### Value Greater Than

`ValueGreaterThanEarlyExit<TKey>` rejects keys that are larger than the maximum observed value. Input `4 9 42 99 123` can yield:

```csharp
if (value > 123)
    return false;
```

#### Value Out Of Range

`ValueOutOfRangeEarlyExit<TKey>` is produced by the optimization pipeline when matching less-than and greater-than bounds can be merged. It rejects keys outside the inclusive observed range. For integral keys this can compile to an unsigned subtraction check equivalent to:

```csharp
if ((uint)(value - min) > (uint)(max - min))
    return false;
```

#### Value In Range

`ValueInRangeEarlyExit<TKey>` rejects gaps between observed ranges by checking if the input is strictly inside a missing interval. Input `10 12 20` can yield:

```csharp
if ((uint)(value - 13) <= (uint)(19 - 13))
    return false;
```

#### Value Bitset

`ValueBitSetEarlyExit<TKey>` packs multiple nearby missing numeric values into one 64-bit bitmap check. The analyzer first finds gaps between consecutive observed ranges. When two or more gap regions fit within a 64-value window, FastData can emit one packed candidate that checks both the bitmap window and the missing-value bits.

For example, observed values `10 12 14 16` have missing values `11`, `13`, and `15`. These gaps fit in the window `[11, 15]`, so they can be represented as one packed check equivalent to:

```csharp
uint diff = (uint)(value - 11);
if (diff <= 4 && ((0b10101UL & (1UL << (int)diff)) != 0))
    return false;
```

The packed bitset is only produced for integral keys. It is considered alongside the individual gap exits and the normal candidate filtering, rejection ratio, and max-candidate selection still apply.

#### Value Bitmask

`ValueBitMaskEarlyExit` builds a mask from bits that are never set by any observed integral key and rejects any key that sets one of them. This is only emitted when the mask is useful and passes the density limit. Input `2 4 6 8` can yield:

```csharp
if ((value & 1) != 0)
    return false;
```

`ValueNotEqualEarlyExit<TKey>` is implemented as an expression type, but it is not currently produced by the numeric early-exit analyzer.

## String Flow

String generation has extra steps because string early exits often use helper functions such as `Length`, `UnitAt`, and `EqualsAt`.

The flow is:

1. String key analysis produces `StringKeyProperties`, including length ranges, character classes, ASCII status, first-unit maps, and last-unit maps.
2. The string structure is selected or created from the user override.
3. `StringEarlyExits.GetExits()` produces analysis-generated candidates.
4. Hash implementations and structures can add mandatory exits.
5. `EarlyExitPipeline.CombineAndDedup()` merges mandatory exits and analysis exits.
6. If `EarlyExitConfig.Optimize` is true, `EarlyExitPipeline.Optimize<string>()` reduces and merges exits.
7. `EarlyExitPipeline.Annotate()` converts exits to expression trees and a `UsedFunctionVisitor` records which generator helper functions they use.
8. If any expression or hash needs string length, `FastDataGenerator` prepends a mandatory `length = Length(key)` allocation.
9. `AllocationGatherTransform` replaces repeated non-boolean `GeneratorFunctions` calls with local variables placed just before first use.
10. `DeduplicateAllocationTransform` removes duplicate method-call allocations while keeping the earliest allocation.
11. If expression optimization is enabled, `EarlyExitPipeline.OptimizeExpressions()` simplifies the expression trees.
12. The expressions are visited again so helper functions introduced by transforms are included in `StringGeneratorConfig`.
13. The language generator emits the final checks into each generated lookup method.

String candidates come from `StringEarlyExits.ProduceCandidates()`.

### String Helper Contract

Generated string early exits are expressed through a small cross-language helper API:

- `UnitAt(value, offset)`
- `UnitAtAsciiLower(value, offset)`
- `Length(value)`
- `EqualsAt(value, offset, fragment)`
- `EqualsAtAsciiLower(value, offset, fragment)`

`Unit` means the selected addressable string unit for the generated target. For byte-oriented targets it is a byte. For UTF-16 code-unit targets it is a UTF-16 code unit.

Offsets are compile-time constants in generated code. `offset >= 0` indexes from the start, and `offset < 0` indexes from the end, with `-1` meaning the last unit. Helper implementations may keep this as a branch in source code because optimized builds should inline the helper and remove the branch for constant offsets.

`AsciiLower` means ASCII-only normalization of `A-Z` to `a-z`. It is not Unicode case folding and it is not culture-sensitive. Implementations should use branch-light ASCII lowering, such as `candidate = unit | 0x20` followed by an unsigned ASCII range check.

Because `AsciiLower` is intentionally ASCII-only, FastData avoids unit and fixed-position string early exits for case-insensitive non-ASCII datasets. Those lookups still use length exits and the final ordinal-ignore-case equality logic.

`EqualsAt` and `EqualsAtAsciiLower` are fixed-position region comparisons used for prefix, suffix, or fragment checks.

### String Early Exit Types

#### Length Less Than

`LengthLessThanEarlyExit` rejects strings shorter than the minimum observed length. Input `horse pig cow sheep` can yield:

```csharp
if (Length(value) < 3)
    return false;
```

#### Length Greater Than

`LengthGreaterThanEarlyExit` rejects strings longer than the maximum observed length. Input `horse pig cow sheep` can yield:

```csharp
if (Length(value) > 5)
    return false;
```

#### Length Not Equal

`LengthNotEqualEarlyExit` is used when all lengths are identical. Input `cow pig cat` can yield:

```csharp
if (Length(value) != 3)
    return false;
```

#### Length Out Of Range

`LengthOutOfRangeEarlyExit` is produced by the optimization pipeline when matching length lower and upper bounds can be merged. It rejects lengths outside the inclusive observed range with an unsigned subtraction check.

#### Length In Range

`LengthInRangeEarlyExit` rejects gaps between observed length ranges by checking for missing length intervals. Input lengths `3 4 7` can yield:

```csharp
if ((uint)(Length(value) - 5) <= 1)
    return false;
```

#### Length Bitmap

`LengthBitmapEarlyExit` builds a 64-bit bitmap of observed lengths and rejects missing lengths in the 1 to 64 range. This is only emitted when the observed length density passes the density limit. Input `stable softice sophisticated santa` can yield:

```csharp
if ((4208UL & (1UL << ((Length(value) - 1) & 63))) == 0)
    return false;
```

#### Unit At Offset Less Than

`UnitAtLessThanEarlyExit` rejects strings whose selected unit is below the minimum observed unit at that offset. Offset `0` is the first unit. Offset `-1` is the last unit. Input `cat dog emu` can yield:

```csharp
if (UnitAt(value, 0) < 'c')
    return false;
```

#### Unit At Offset Greater Than

`UnitAtGreaterThanEarlyExit` rejects strings whose selected unit is above the maximum observed unit at that offset. Input `cat dog emu` can yield:

```csharp
if (UnitAt(value, 0) > 'e')
    return false;
```

#### Unit At Offset Not Equal

`UnitAtNotEqualEarlyExit` is used when all strings share the same selected unit. Input `apple axe ant` can yield:

```csharp
if (UnitAt(value, 0) != 'a')
    return false;
```

When ignore-case is enabled this uses `UnitAtAsciiLower`.

#### Unit At Offset Out Of Range

`UnitAtOutOfRangeEarlyExit` is produced by the optimization pipeline when matching unit lower and upper bounds with the same offset can be merged. It rejects selected units outside the inclusive observed range.

#### Unit At Offset In Range

`UnitAtInRangeEarlyExit` rejects missing interior ranges in observed ASCII units at the selected offset.

#### Unit At Offset Bitmap

`UnitAtBitmapEarlyExit` builds a bitmap of observed selected units and rejects missing ASCII units. This is only emitted when the observed unit density passes the density limit. Input `alpha zulu` yields a bitmap test for `UnitAt(value, 0)`. When ignore-case is enabled this uses `UnitAtAsciiLower`.

#### Equals At Offset

`EqualsAtEarlyExit` rejects strings that do not contain an observed fragment at a fixed offset. Offset `0` acts as a prefix check, and a negative offset acts as a suffix check. Input `preOne preTwo preSix` can yield:

```csharp
if (!EqualsAt(value, 0, "pre"))
    return false;
```

Input `OneSuf TwoSuf SixSuf` can yield:

```csharp
if (!EqualsAt(value, -3, "Suf"))
    return false;
```

When ignore-case is enabled this uses `EqualsAtAsciiLower`.

## UnitAt Safety Guard

`UnitAt` exits read a first or last string unit. They are only safe when an earlier length check has rejected empty or too-short inputs.

`StringEarlyExits.EnsureUnitAtLengthGuard()` enforces this rule. If any selected exit needs non-empty input, the function ensures a rejecting length guard is placed before those unit exits. If it cannot add such a guard, it removes the unsafe unit exits.

This ordering is important because later pipeline stages preserve the relative order of generated checks.

## Combining And Deduplication

`EarlyExitPipeline.CombineAndDedup()` accepts two sequences. In normal generation, the first sequence is mandatory exits and the second sequence is analysis-generated exits.

The function appends exits in order and skips duplicates using record value equality. This means a mandatory exit wins over an identical analysis exit because it appears first.

Order matters because generated code runs early exits in the resulting order. Mandatory safety checks should therefore be produced before checks that depend on them.

## Pipeline Optimization

`EarlyExitPipeline.Optimize<TKey>()` has two phases.

The reduction phase removes an exit when `current.IsWorseThan(other)` says another exit is strictly stronger. For example, `Length(key) < 3` is weaker than `Length(key) < 5` because every string rejected by the first check is also rejected by the second.

The merge phase combines complementary lower and upper bounds into one out-of-range check:

- `ValueLessThanEarlyExit<TKey>` plus `ValueGreaterThanEarlyExit<TKey>` becomes `ValueOutOfRangeEarlyExit<TKey>`.
- `LengthLessThanEarlyExit` plus `LengthGreaterThanEarlyExit` becomes `LengthOutOfRangeEarlyExit`.
- Matching `UnitAtLessThanEarlyExit` and `UnitAtGreaterThanEarlyExit` offsets become `UnitAtOutOfRangeEarlyExit`.

The range-based exits use unsigned subtraction for integral values and string lengths. For example, an out-of-range check can become the equivalent of `(uint)(key - min) > (uint)(max - min)`, which rejects values outside `[min, max]` with one comparison. An in-range gap check uses the inverse form, such as `(uint)(key - (min + 1)) <= (uint)(max - min - 2)`.

This subtraction form is intentional: it packs the lower-bound and upper-bound tests into one comparison for integral ranges, which is cheaper than emitting `key < min || key > max` when unsigned arithmetic is available.

When two list entries are merged, the later index must be removed first. Removing the earlier index first shifts the later index and can remove the wrong item or throw.

## Expression Optimization

`EarlyExitPipeline.OptimizeExpressions()` runs expression-tree simplification after exits have been annotated and, for string keys, after allocation transforms have run.

The optimizer can fold constants, simplify boolean algebra, reduce duplicate comparisons, and simplify arithmetic or bitwise patterns. It keeps the `AnnotatedExpr.Kind` unchanged so assignments remain assignments and early-exit conditions remain early-exit conditions.

## Method Header Emission

Every structure template calls `GetMethodHeader()` at the start of generated `Contains` and `TryLookup` methods.

`GetMethodHeader()` does the last transformation step. It applies `EarlyExitConditionTransform` to every annotated expression:

- Assignments remain assignments, such as `int length = Length(key);`.
- Early-exit conditions become method-specific guard statements.

For `Contains`, the body is equivalent to:

```csharp
if (condition)
    return false;
```

For `TryLookup`, the body is equivalent to:

```csharp
if (condition)
{
    value = default;
    return false;
}
```

After this transform, the language-specific expression compiler renders the block into C#, C++, or Rust source code.

## Helper Function Emission

String early exits are expressed with `GeneratorFunctions` calls rather than language-specific source text. Examples include `Length`, `UnitAt`, `UnitAtAsciiLower`, `EqualsAt`, and `EqualsAtAsciiLower`.

`UsedFunctionVisitor` records which helper functions are present in the final expression trees. `StringGeneratorConfig` carries this flag set to the language generator. Template headers then emit only the helper functions needed by the generated code.

Ignore-case lookup helpers, such as case-insensitive compare and equality in C++ and Rust, are emitted when the string generator config has `IgnoreCase = true`.

## End-To-End Example

Given numeric keys `[10, 11, 12, 20]`, numeric analysis sees the observed range `[10, 20]` and the gap `(12, 20)`. Candidate generation can produce:

- `ValueLessThanEarlyExit<int>(10)`
- `ValueGreaterThanEarlyExit<int>(20)`
- `ValueInRangeEarlyExit<int>(12, 20)`

After combination and optimization, the lower and upper bounds can merge into `ValueOutOfRangeEarlyExit<int>(10, 20)`. The generated method header rejects keys outside `[10, 20]` before running the selected structure lookup, and can also reject keys strictly between `12` and `20` if the gap exit survives scoring.

Given string keys `["cat", "dog", "emu"]`, string analysis sees length `3`, first units from `c` to `e`, and last units from `g` to `t`. Candidate generation can produce:

- `LengthNotEqualEarlyExit(3)`
- `UnitAtLessThanEarlyExit('c', 0)`
- `UnitAtGreaterThanEarlyExit('e', 0)`

If the unit checks survive scoring, the length guard is kept before them. During template rendering, those conditions become early `return false` checks before the main lookup structure runs.