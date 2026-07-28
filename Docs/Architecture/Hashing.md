# Hashing

FastData uses hashing in two places:

* numeric keys use a mostly direct numeric hash path.
* string keys can use a generated hash function selected from several candidates.

This document focuses on the string hash generation framework. The central implementation is in `Src/FastData/Internal/HashBenchmark.cs`, `Src/FastData/Internal/Analysis/Analyzers`, `Src/FastData/Generators/StringHash`, and `Src/FastData/Internal/Analysis/Expressions/ExpressionHashBuilder.cs`.

## Pipeline

String hash generation runs only when a selected string structure needs hash data, such as `BloomFilter`, `ConstMap`, `HashTable`, `HashTableCompact`, `HashTablePerfect`, or `Hyble`.

The flow is:

1. `KeyAnalyzer.GetStringProperties()` analyzes encoded string data and records length ranges, minimum and maximum encoded byte lengths, ASCII status, character classes, first and last character maps, and byte-level delta maps.
2. `FastDataGenerator` calls `EnsureHashData()` lazily during string structure selection or structure creation.
3. If `StringDataConfig.StringAnalyzerConfig` is enabled, `HashBenchmark.GetBestHash()` gathers candidates from the configured analyzers plus the default hash.
4. If string analysis is disabled, `DefaultStringHash` is used directly.
5. The selected `IStringHash` produces an `Expression<StringHashFunc>`.
6. The expression is compiled in the core pipeline to compute `HashData` for the input keys.
7. The same expression and any extra static data are carried in `StringHashInfo` so target-language generators can render a matching generated `Hash()` function.

This matching is important: the core pipeline hashes the input keys at generation time, and the generated lookup code hashes user input at runtime. Both sides must use the same expression and encoding semantics.

## Shared Contracts

`IStringHash` is the common contract for generated string hashes. It provides:

* `GetExpression()`: the hash expression over `byte[] data` and `int length`.
* `AdditionalData`: optional static arrays required by the generated hash, currently used by `GPerfStringHash` for association values.
* `GetMandatoryExits()`: required early exits that must run before the lookup, such as minimum length or ASCII-only checks.

`StringHashFunc` is `ulong StringHashFunc(byte[] value, int length)`. All string hash candidates operate on encoded bytes rather than `string` characters. The generator encoding (`AsciiBytes`, `Utf8Bytes`, or `Utf16CodeUnits`) decides how source strings are converted before analysis, simulation, and generated lookup.

## Candidate Evaluation

`HashBenchmark.GetBestHash()` always includes `DefaultStringHash` when `includeDefault` is true. It then runs each configured analyzer whose `IsAppropriate()` returns true:

* `PositionLengthAnalyzer`
* `BruteForceAnalyzer`
* `GeneticAnalyzer`
* `GPerfAnalyzer`

Each analyzer returns one or more `Candidate` objects. A candidate contains the `IStringHash`, a fitness score, a collision count, and optionally a benchmark time.

`Simulator` evaluates a candidate by compiling its expression and inserting every encoded key into a bucket array without doing equality checks. A second item landing in an occupied bucket counts as a collision. Fitness starts as `(capacity - collisions) / capacity`, where capacity is the simulated bucket count. Brute-force and genetic candidates also add an extra fitness term from `FitnessHelper` that favors shorter byte segments and smaller expressions.

Selection considers perfect candidates first but still measures speed:

1. candidates are split into collision-free and colliding groups.
2. each perfect candidate is benchmarked by repeatedly hashing a byte buffer.
3. the best colliding candidate is benchmarked too, when one exists.
4. the fastest perfect candidate wins if it is at least as fast as the best colliding candidate.
5. otherwise, the colliding candidate wins when its measured time is below `perfectTime + (perfectTime * PerfectHashThreshold)`; with the current default formula, any faster colliding candidate satisfies this check.

The default `PerfectHashThreshold` is `0.25`. The code comments describe this as a perfect-hash preference threshold, but the implemented comparison should be read as the source of truth.

After selection, `HashData.Create()` computes final hash codes and the final table size using `HashTableCapacityFactor` and optional power-of-two modulo rounding. `HashData.HashCodesPerfect` means there are no collisions after applying the final table modulo, which allows `StringStructures` to choose `HashTablePerfect`.

## Default Hash

`DefaultStringHash` is the safe baseline and fallback. It hashes the whole encoded input one unit at a time with a DJB2-like mixer:

* initial seed: `352654597`
* mixer: `(((hash << 5) | (hash >> 27)) + hash) ^ read`
* avalanche: `352654597 + (hash * 1566083941)`

It is available for ASCII bytes, UTF-8 bytes, and UTF-16 code units. When `IgnoreCase` is enabled it applies the same byte-level case folding in the expression builder as the other expression-based hashes.

The default hash has no additional data and no mandatory early exits.

## Position/Length Analyzer

`PositionLengthAnalyzer` emits a small fixed set of simple hashes based on the first encoded unit, the last encoded unit, and the encoded input length. It is intended as a cheap candidate source for common keyword sets where edge characters and length distinguish many keys.

By default it returns these permutations:

* length only.
* first character.
* first character plus length.
* last character.
* last character plus length.
* first and last character.
* first and last character plus length.

The analyzer prunes candidates that cannot distinguish the analyzed keys. Length variants are emitted only when encoded lengths vary. First-character variants are emitted only when first characters vary, and last-character variants are emitted only when last characters vary. When every key is exactly one encoded unit long, last-character candidates are skipped because the first and last positions are equivalent.

`PositionLengthAnalyzerConfig.IncludeLength` disables length-contribution variants when set to `false`. `PositionLengthAnalyzerConfig.IncludeLastChar` disables last-character variants when set to `false`. Disabling both leaves only useful first-character candidates, which avoids the extra length and tail-position work that can be expensive in generated C-like languages.

Set `StringAnalyzerConfig.PositionLengthAnalyzerConfig` to `null` to disable this analyzer entirely.

## Brute-Force Analyzer

`BruteForceAnalyzer` tries a bounded Cartesian product of:

* fixed string byte segments from `BruteForceGenerator(8)`.
* mixer operations such as identity, add, subtract, xor, multiply, rotates, xor-shift, and square.
* avalanche operations such as identity, multiply by Murmur-derived seeds, and xor-right-shift.

The brute-force segment generator emits left-aligned and right-aligned fixed segments whose offset and length fit inside the shortest encoded key, capped at 8 bytes. This makes the search deterministic and bounded.

The analyzer reuses a `BruteForceStringHash` object while iterating. For each combination it runs the simulator and pushes the best candidates into a fixed-size `MinHeap`. It stops when it reaches maximum fitness or `BruteForceAnalyzerConfig.MaxAttempts`. `MaxReturned` controls how many top candidates are returned to the benchmark selector.

This analyzer is useful when a small byte slice and a simple mixer can already distinguish the dataset.

## Genetic Analyzer

`GeneticAnalyzer` searches the same broad design space without exhaustively enumerating it. It uses a generic genetic engine with genes for:

* selected byte segment.
* mixer random seed.
* mixer iteration count.
* avalanche random seed.
* avalanche iteration count.

Segments come from `SegmentManager`, which combines `BruteForceGenerator`, `EdgeGramGenerator`, `DeltaGenerator`, and `OffsetGenerator`, then removes duplicates. This gives the genetic search fixed edge segments, high-delta byte regions, and full-tail segments.

The generated hash is `GeneticStringHash`. Its mixer and avalanche are built from deterministic `Random` instances seeded by the genes. The available operations include add, subtract, multiply, xor, Murmur-derived multiply constants, rotates, xor-shifts, and square mixing.

The engine currently uses tournament selection, one-point crossover, uniform mutation, elite reinsertion, and max-generation termination. The public knobs are `PopulationSize`, `MaxGenerations`, `ShuffleParents`, and `RandomSeed`.

This analyzer is useful when the best segment and operation mix is not obvious from local exhaustive search.

## GPerf Analyzer

`GPerfAnalyzer` implements the core of GNU gperf-style perfect hashing. It is based on Cichelli-style associated values and searches for projections that are injective for the keyword set:

1. selected byte positions produce distinct tuples.
2. alpha increments turn those tuples into distinct multisets.
3. association values turn the multisets into distinct hash values.

The generated hash is `GPerfStringHash`. It starts from the input length unless `NoLength` is set, then adds `AssociationValues[...]` entries for selected positions. Positions are sorted in gperf order and may include `$`, represented internally as `-1`, for the last byte.

The analyzer supports gperf-like configuration:

* `MaxPositions`: maximum automatic fixed position to consider.
* `KeyPositions`: explicit positions such as `1,4,$`, ranges such as `1-8`, or `*` for all supported dataset positions.
* `SevenBit`: require 7-bit bytes.
* `NoLength`: omit the length contribution.
* `InitialAssociationValue`, `Jump`, `MultipleIterations`, `Random`, `RandomSeed`, and `SizeMultiple`: control association table search.

FastData validates these options more strictly than the gperf CLI. Invalid negative values, unsupported positions, and duplicate key-position selections throw instead of being silently clamped or ignored.

`GPerfAnalyzer.IsAppropriate()` requires a known generator encoding. It also requires ASCII data for ASCII-byte generation and for case-insensitive gperf hashing. It returns no candidates for empty encoded strings or for `SevenBit` data that contains bytes above `0x7f`.

`GPerfStringHash` can add mandatory early exits. It adds a minimum-length early exit when the generated hash assumes at least one byte, and it adds an ASCII-only early exit for ASCII-byte or seven-bit hashes. Its association table is emitted through `AdditionalData` so templates can generate a static array alongside the hash function.

## Expression Hash Builder

`ExpressionHashBuilder` converts hash specifications into expression trees. There are two paths:

* `BuildOneByOne()` emits the simple serial loop used by `DefaultStringHash`.
* `Build()` emits segment-based hashes used by brute-force and genetic hashes.

For fixed-size segments, `Build()` emits straight-line reads in 8, 4, 2, and 1 byte chunks. Left-aligned segments read from a constant offset. Right-aligned segments compute the offset from `length`.

A segment length of `-1` means a full-tail segment: read from the segment offset through the end of the encoded input. `OffsetGenerator` emits these segments for the genetic analyzer.

Full-tail hashing uses an advanced loop for large inputs. When at least 32 bytes remain, the builder creates four independent 64-bit lanes (`v1` through `v4`). Each loop iteration performs four independent 8-byte reads and mixer updates, then merges the lanes into the main hash. This exposes instruction-level parallelism to the JIT or target compiler because the four lane updates do not depend on each other. After the 32-byte loop, the builder handles remaining 8, 4, 2, and 1 byte tails normally.

Case-insensitive expression hashes apply ASCII byte folding by OR-ing each read chunk with `0x20` masks. This is intentionally byte-level and must match between generation-time hashing and runtime hashing.

## Code Generation Boundary

`FastDataGenerator` stores the selected hash expression and additional data in `StringHashInfo`. Language generators render this into the generated source. In the C# generator, `Functions.ttinclude` renders the expression into a private `Hash()` method, and `Header.ttinclude` emits any `AdditionalData` arrays.

Hash structures call this generated `Hash()` method and then apply the structure-specific modulo or multiplier logic. Equality checks remain separate from hashing, so non-perfect hash tables can still resolve collisions by comparing stored keys.

## Configuration

`StringAnalyzerConfig` enables all four analyzers by default and controls final benchmarking:

* `BenchmarkIterations`: number of repeated hash calls used for timing candidates.
* `PerfectHashThreshold`: fixed at `0.25` in the current public config.
* analyzer-specific config objects can be set to `null` to disable individual analyzers.

The CLI maps analysis levels to this config:

* `Disabled`: no analyzer config; use `DefaultStringHash` directly.
* `Fast`: reduced brute-force attempts, smaller genetic population and generation count, and lower gperf `MaxPositions`.
* `Balanced`: default `StringAnalyzerConfig`.
* `Aggressive`: full brute-force attempts, larger genetic search, and full gperf position range.

## Practical Implications

The framework is optimized for generated lookup code rather than general-purpose hashing:

* it only needs to perform well for the known input set and expected misses.
* perfect modulo distribution is more valuable than general avalanche quality when `HashTablePerfect` is possible.
* position/length hashes are fast to evaluate, prune low-value permutations, and can be restricted to first-character-only when length or last-character access is expensive in the target language.
* short segments can beat whole-string hashes when they are distinctive enough.
* gperf can produce very compact and fast hashes, but only when its position and association search succeeds.
* the default hash keeps generation reliable when analysis is disabled or analyzers fail to improve on the baseline.