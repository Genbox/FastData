# FastData

FastData generates data-dependent lookup code for static, read-only datasets. Because the complete key set is known before the generated code runs, FastData spends time on analysis and emit a specialized structure instead of a general-purpose runtime collection.

The generated code supports two query shapes:

* Membership: Returns whether a key exists.
* Key/value lookup: Maps a key to a value.

# Data types

FastData supports two key families: numeric keys and string keys.

Numeric key generation supports these key types:

* `sbyte`, `byte`
* `short`, `ushort`
* `int`, `uint`
* `long`, `ulong`
* `char`
* `float`, `double`

Floating-point keys must be finite. `NaN`, positive infinity, and negative infinity are rejected during validation because exact generated lookup code cannot treat them as ordinary ordered keys.

String key generation supports `string` keys. String lookups are ordinal by default and can be configured for ordinal ignore-case matching with `IgnoreCase`. Ignore-case support is only valid for string keys.

Key/value generation supports a value array alongside the key array. Value support depends on the selected target-language generator because values must be representable as generated source literals or generated data in that language.

## Pipeline

The high-level generation pipeline is:

1. Validate input keys and optional values.
2. Deduplicate keys.
3. Analyze key properties.
4. Select a structure, unless the caller forced one.
5. Build any structure-specific data, such as hash tables or bit vectors.
6. Generate early-exit checks from the analyzed properties.
7. Create a generator context for the selected structure.
8. Render language-specific source code from templates.

The public entry point is `FastDataGenerator`.

# Analysis

## Numeric keys analysis

* Minimum and maximum values.
* Sorted ranges and number of ranges.
* Value density across the observed range.
* Whether zero appears in the dataset.
* Missing-bit masks used by early exits.

## String keys analysis

* Minimum and maximum character length.
* Length ranges and whether lengths are unique.
* Character classes and ASCII compatibility.
* First-character and last-character distributions.

When string hash analysis is enabled, FastData benchmarks candidate hash expressions against the actual key set.

# Early exits

Early exits are cheap checks emitted before the main lookup. They reject impossible keys quickly, such as strings with missing lengths or numbers outside the observed range.

See [EarlyExits.md](Architecture/EarlyExits.md) for details.

# Data structures

FastData selects a lookup structure from the analyzed shape of the key set. Dense numeric ranges might use compact range or bit-set structures, while sparse or irregular data might use binary search, hash tables, perfect hashing, or conditional branches. String data can also use length-based structures or hash strategies when those are a better fit.

The selected structure determines the data stored in the generated code and the final lookup path after early exits have run. Users can let FastData choose automatically or force a structure through configuration when they know which tradeoff they want.

See [DataStructures.md](Architecture/DataStructures.md) for details.

# Hashing

Hashing is used when a hash-based structure is the best fit for the analyzed key set. For strings, FastData can benchmark candidate hash expressions against the actual keys and choose a hash strategy that reduces collisions for the generated lookup.

Hashing decisions are still compile-time work: the generated code only contains the selected hash calculation and the prepared lookup data needed by the chosen structure.

See [Hashing.md](Architecture/Hashing.md) for details.

# Misc

## Performance optimizations

FastData performs several compile-time and generated-code optimizations, including structure selection, early exits, expression simplification, branch-shape choices, compact table layouts, and arithmetic reductions. These optimizations aim to move expensive decisions into generation time while keeping runtime lookup code small and predictable.

See [Optimizations.md](Misc/Optimizations.md) for details.

## Compile-time benefits

Generating the structure ahead of time provides several benefits:

* Data can be compiled into the consuming program.
* Expensive analysis has zero runtime cost.
* The generated code avoids defensive copying and runtime collection initialization.
* Calls can avoid virtual dispatch and unnecessary branching.
* Modulo by known constants can be optimized by the compiler.
* Internal tables can use smaller integer types when the data range allows it.
* String data can use the target language's most efficient representation.