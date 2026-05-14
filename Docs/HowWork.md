# How FastData Works

FastData generates data-dependent lookup code for static, read-only datasets. Because the complete key set is known before the generated code runs, FastData can spend time on analysis and emit a specialized structure instead of a general-purpose runtime collection.

The generated code supports two query shapes:

* Membership: `Contains(key)` returns whether a key exists.
* Key/value lookup: Maps a key to a value.

## Pipeline

The high-level generation pipeline is:

1. Validate input keys and optional values.
2. Deduplicate keys according to the configured duplicate policy.
3. Analyze key properties.
4. Select a structure, unless the caller forced one.
5. Build any structure-specific data, such as hash tables or bit vectors.
6. Generate early-exit checks from the analyzed properties.
7. Create a generator context for the selected structure.
8. Render language-specific source code from templates.

The public entry point is `FastDataGenerator`. It accepts a data config, an `ICodeGenerator`, and the input data. The selected structure creates an `IContext`, and the language generator renders the matching T4 template for C#, C++, or Rust.

## Numeric keys

Numeric generation starts with `NumericDataConfig` and supports integral types, `char`, `float`, and `double`. Floating-point keys cannot contain `NaN` or infinity.

Numeric analysis derives:

* Minimum and maximum values.
* Sorted ranges and number of ranges.
* Value density across the observed range.
* Whether zero appears in the dataset.
* Missing-bit masks used by early exits.

## String keys

String generation starts with `StringDataConfig`. Empty and null string keys are rejected. Generators that expose an ASCII-byte API require ASCII-compatible input.

String analysis derives:

* Minimum and maximum character length.
* Length ranges and whether lengths are unique.
* Character classes and ASCII compatibility.
* First-character and last-character distributions.
* Common prefix and suffix data when trimming is enabled.

When string hash analysis is enabled, FastData benchmarks candidate hash expressions against the actual key set. It can use brute force, GPerf-style position selection, and genetic search to find a good hash for the dataset.

## Early exits

Early exits are cheap checks emitted before the main lookup. They reject impossible keys quickly, such as strings with missing lengths or numbers outside the observed range.

Early-exit candidates are controlled by `EarlyExitConfig`:

* `Disabled` turns off generated early exits.
* `MinItemCount` avoids using the early exit for very small datasets.
* `MaxCandidates` limits how many candidates survive selection.
* `MinRejectionRatio` filters out checks that reject too little of the observed range.

Some structures, such as `Range` and `SingleValue`, already encode their own checks and therefore do not use generated early exits.

## Compile-time benefits

Generating the structure ahead of time provides several benefits:

* Data can be compiled into the consuming program.
* Expensive analysis has zero runtime cost.
* The generated code avoids defensive copying and runtime collection initialization.
* Calls can avoid virtual dispatch and unnecessary branching.
* Modulo by known constants can be optimized by the compiler.
* Internal tables can use smaller integer types when the data range allows it.
* String data can use the target language's most efficient representation.
