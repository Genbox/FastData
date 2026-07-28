# Data structures

FastData selects a structure automatically by default. Callers can also force a structure through the CLI, source generator attributes, or library configuration where that surface exposes the structure.

Automatic selection is data-dependent. The important inputs are key count, key type, density, range count, whether values are attached, whether approximate matching is allowed, and whether hash codes are collision-free for the dataset.

## Selection summary

Numeric keys are considered in this order:

1. `SingleValue`
2. `Range`
3. `BloomFilter`
4. `BitSet`
5. `Conditional`
6. `RrrBitVector`
7. `EliasFano`
8. `HashTablePerfect`
9. `HashTable`

String keys are considered in this order:

1. `SingleValue`
2. `BloomFilter`
3. `KeyLength`
4. `Conditional`
5. `HashTablePerfect`
6. `HashTable`

`StructureConfig.Default` controls most automatic thresholds. The defaults favor simple branch-based code for small datasets, dense bit-level structures for suitable integral data, and hash tables for larger or irregular data.

## Complexity and memory model

Complexity below is the amortized time for a single query after generated code has been loaded. Memory describes the generated lookup data for keys and lookup metadata only. Value storage is intentionally ignored, even for structures that support key/value lookup. The formulas also ignore executable instructions, method metadata, array/object headers, alignment padding, and target-runtime string/object overhead. Those costs differ by language and runtime, but the listed fields are the actual data emitted by the current structures/templates.

`n` means the number of stored keys. Average memory assumes large `n`, so word-rounding effects such as `ceil(... / 64)` are ignored. For fixed-width numeric keys, one key slot is 8 bits for `byte`/`sbyte`, 16 bits for `char`/`short`/`ushort`, 32 bits for `int`/`uint`/`float`, and 64 bits for `long`/`ulong`/`double`. For strings on a 64-bit target, use 64 bits/key for a C# string reference or 128 bits/key for a C++ `std::string_view` / Rust `&str`, then add string literal payload bits.

`TypeReductionEnabled` is enabled by default. When enabled, generated integral metadata uses the smallest compatible built-in fixed-width type that covers the actual emitted values. Unsigned fields remain unsigned, while sentinel-bearing and signed-arithmetic fields retain signed storage; nonnegative GPerf association values may switch from signed to unsigned storage. This includes indexes, offsets, displacements, cached hashes, range boundaries, model intercepts, and packed-word arrays. Lookup code widens reduced values before arithmetic or bit operations. Disabling type reduction preserves each structure's canonical metadata types.

## Array

* Supports: Numeric and string keys
* Values: Yes
* Complexity: O(n)
* Compressed: No

`Array` emits a linear scan over the keys. It is mostly useful when explicitly requested or as a simple baseline. Early exits can make it faster than a plain runtime array lookup for rejected keys.

## BinarySearch

* Supports: Numeric and string keys
* Values: Yes
* Complexity: O(log n)
* Compressed: No

`BinarySearch` sorts the keys at generation time and emits binary-search logic. It is useful when predictable memory usage matters and logarithmic lookup cost is acceptable.

## BinarySearchInterpolation

* Supports: Numeric keys
* Values: Yes
* Complexity: O(log log n) on evenly distributed data; worst-case O(n)
* Compressed: No

`BinarySearchInterpolation` uses the numeric value distribution to estimate the next probe location. It is useful for numeric datasets with a relatively even spread.

## BitSet

* Supports: Integral numeric keys
* Values: Yes
* Complexity: O(1)
* Compressed: Yes: `64 * ceil(R / 64)` bits, where `R = (max - min) + 1`

`BitSet` maps a key to a bit position inside the observed numeric range. With values, the generated table also stores a dense value array indexed by numeric offset, but value memory is not included in the formula above.

## BloomFilter

* Supports: Numeric and string keys
* Values: No
* Complexity: O(1)
* Compressed: Yes: `64 * ceil((10 * n) / 64)` bits

`BloomFilter` is selected only when approximate matching is allowed. The current implementation allocates about 10 bits per key, then sets two bits inside one selected 64-bit word. The word index and both bit positions are derived from the same hash value, so the false-positive rate is closer to 3-5% on typical large sets than the roughly 1% expected from an optimal 10-bit/key Bloom filter. It is valid for compact membership pre-filtering when false positives are acceptable, but not for exact key/value lookup or callers that require a specific false-positive bound. `RoundModuloToPowerOfTwo` is enabled by default, so the bitset word length can be rounded up to the next power of two if the extra word count is within `RoundModuloToPowerOfTwoThreshold`. The threshold is a ratio, so the default `0.125` allows up to 12.5% extra slots.

## Conditional

* Supports: Numeric and string keys
* Values: Yes
* Complexity: O(n)
* Compressed: No

`Conditional` emits language-level conditions or switches. It is the default choice for small datasets because compilers can optimize the emitted branches well. The default item-count limit is conservative because branch-heavy code becomes slower as the set grows.

## ConstMap

* Supports: Numeric and string keys
* Values: Yes
* Complexity: O(1)
* Compressed: Yes. Approximately `1.125 * n * candidateBits` bits for large datasets

`ConstMap` uses a binary-fuse XOR table to recover a candidate ordinal with one generated base hash, a seeded finalizer, and three table reads. The ordinal indexes a compact key or key/value entry. Generated code bounds-checks the ordinal and compares the complete key, so membership and key/value misses are exact rather than fingerprint-based. This also allows arbitrary generated value types instead of restricting values to XOR-compatible integers. The construction follows [Binary Fuse Filters: Fast and Smaller Than Xor Filters](https://arxiv.org/abs/2201.05041).

The canonical context stores 32-bit cells. With type reduction enabled, generated cells use the smallest unsigned built-in type covering the constructed table values, commonly 8 bits through 256 keys, 16 bits through 65,536 keys, and 32 bits above that. Key and value storage is excluded from the formula, consistent with the memory model above.

Construction is deterministic and retries bounded seeds when the binary-fuse graph cannot be peeled. It fails when two stored keys have the same generated 64-bit base hash because no seeded permutation can separate them. ConstMap is currently available only when explicitly requested; automatic selection continues to use the established structures until cross-language benchmark thresholds are available.

## EliasFano

* Supports: Integral numeric keys
* Values: No
* Complexity: O(1 + local high-bucket scan), with samples bounding zero-selection work; worst-case O(n) for highly clustered values
* Compressed: Yes. `64 * ceil(upperBitLength / 64) + 64 * ceil((n * lowerBitCount) / 64) + (32 * sampleCount)` bits

`EliasFano` stores sparse monotonic integer sets in a compressed representation. SkipQuantum` is 128 by default. It stores upper bits, optional lower bits, and 32-bit sample positions.

## HashTable

* Supports: Numeric and string keys
* Values: Yes
* Complexity: O(1) amortized; worst-case O(n) if all entries collide into one bucket
* Compressed: No

`HashTable` is the general fallback for large or irregular datasets. It emits a bucket array and an entry array. Bucket indexes use the smallest unsigned type covering the actual bucket values, while `Next` uses the smallest signed type covering its actual values and the `-1` sentinel. Cached hashes use the smallest unsigned type covering the generated hashes. FastData first computes the base bucket length as `ceil(n * HashTableCapacityFactor)`. `OptimizeHashTableBucketSize` is enabled by default; when enabled, FastData treats the base length as a starting point and scans a bounded set of larger candidate bucket counts, choosing a size with fewer or acceptable collisions. `RoundModuloToPowerOfTwo` is enabled by default and runs after bucket-size optimization, so `B` can be rounded up to the next power of two if the extra bucket count is within `RoundModuloToPowerOfTwoThreshold` and rounding does not increase collisions. Each entry stores the key, a `Next` index for separate chaining, and an optional hash code. Numeric keys use identity-style hashing where possible. String keys use either the default string hash or a dataset-specific hash expression when analysis is enabled.

## HashTableCompact

* Supports: Numeric and string keys
* Values: Yes
* Complexity: O(1) amortized; worst-case O(n) if one bucket contains all entries
* Compressed: No

`HashTableCompact` stores two bucket arrays, `bucketStarts` and `bucketCounts`, then stores all entries for a bucket contiguously. Here `B`, `I(x)`, and `H` have the same meaning as in `HashTable`. Entries do not need a `Next` index, so this saves per-entry memory when the two bucket arrays are cheaper than storing one next pointer per entry.

## HashTablePerfect

* Supports: Numeric and string keys
* Values: Yes
* Complexity: O(1)
* Compressed: No

`HashTablePerfect` is selected when all generated hash codes are unique for the dataset. Here `B` is the entry table length, normally `n * HashTableCapacityFactor`, with the same optional power-of-two rounding as `HashTable`. It emits one entry array indexed directly by `hash % B`, with no bucket array and no collision-chain metadata. If `HashTableCapacityFactor` or modulo rounding creates empty slots, or the key type does not use identity hashing, entries also store a cached hash using the smallest unsigned type that covers all hashes and the empty-slot sentinel.

## KeyLength

* Supports: String keys
* Values: Yes
* Complexity: O(1)
* Compressed: No

`KeyLength` uses string length as the index when every key length is unique and the length range is dense enough. The generated key table has one slot for every length in `[minLength, maxLength]`, so gaps still consume key slots. Empty length slots do not add string payload. With values, it also emits a type-reduced unsigned offset array of length `L` and a compact value array of length `n`, but value-related storage is not included above.

## Range

* Supports: Numeric keys
* Values: No
* Complexity: O(r) for fewer than eight ranges; O(log r) otherwise
* Compressed: Yes. `2 * r * boundaryBits` where `r` is the number of ranges and `boundaryBits` is the emitted boundary width (`keyBits` when type reduction is disabled)

`Range` stores consecutive integral keys as ranges. For a single range, the generated code embeds the start/end constants directly in the comparison. For multiple ranges, it emits separate start and end arrays using the smallest same-signedness type that covers every boundary. Membership uses an early-terminating linear scan for fewer than eight ranges and binary-searches the range starts otherwise.

Automatic selection requires membership-only integral data and applies the configured range-count limits. A single range is always accepted because it is emitted as two constants. Multiple ranges are accepted only when their `2 * r` endpoints use fewer key slots than the original `n` keys (`2 * r < n`), preventing mostly-singleton datasets from being expanded by range encoding.

## RrrBitVector

* Supports: Integral numeric keys
* Values: No
* Complexity: O(1), with a fixed 15-bit block decode
* Compressed: Yes. `40 * C` bits, where `U = (max - min) + 1` and `C = ceil(U / 15)`

`RrrBitVector` stores very sparse integer sets as a compressed bit vector. The current implementation uses 15-bit blocks, one 8-bit class per block, and one 32-bit offset per block. It does not store the original keys.

## SingleValue

* Supports: Numeric and string keys
* Values: Yes
* Complexity: O(1)
* Compressed: No

`SingleValue` is selected when the dataset contains one unique key. It emits a direct equality check instead of a full collection structure.

# Comparison

The graph below shows which data structure is the fastest for a given number of keys in one benchmark scenario. The Y-axis is the number of queries per second (QPS). The X-axis is the number of keys.

![StructuresGraph.png](StructuresGraph.png)

Use the graph as a broad intuition, not a fixed rule. FastData's automatic selection also accounts for data shape, key type, density, ranges, values, and configured limits.