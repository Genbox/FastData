# Optimizations
FastData contains a lot of optimizations that are not that obvious from the get-go. I'll try and document them here.

## Reductions
A reduction is the process of going from a generalized data structure, to a specialized data structure that is much faster and more efficient.

### Reduction to single element lookup
If you create a hash table with a single element in any programming language, the hash table still have to hash inputs, match it to a bucket and do equality.
But since there is just one element, we don't have to do all that. FastData will return a specialized data structure that just check against the single element.

### Reduction to range lookup
If the input is numeric and dense, we can represent it as a simple range check, rather than a more complex data structure.

### Reduction to length lookup
When indexing strings, you have to work with a hash function to make the string into an integer. That can be a pretty heavy operation that will dominate the time it takes to lookup an element.
Can we do better? Let's say you have an input like this: `house`, `car` and `fish`.

In this case, each of the input strings have a unique length. So why not use their length as their hash? That's exactly what `KeyLengthStructure` does.
If the programming language cache the string length, we can get O(1) lookups.

### Reduction to bitmap lookup
If a sequence of numbers is a short range, but cannot get optimized via the range reduction above, then it might be a good fit for a bitmap instead.
Range reduction works when the numbers can be represented as a dense set of ranges, and there are too many numbers to use conditionals efficiently, so instead we use a bitmap.

## Early exits
An early exit is a lightweight check we can perform before the actual lookup to speed up the process. They have to be **really** fast, otherwise they will add unwanted overhead to each call.
Early exits come from two places: analysis-generated candidates and structure-mandated checks. The default configuration disables analysis-generated early exits for `RangeStructure` and `SingleValueStructure` because their lookup logic already includes the needed bounds/equality checks. Some other structures can still add mandatory exits; for example `KeyLengthStructure` adds length bounds, and `RrrBitVectorStructure`/`EliasFanoStructure` add numeric min/max bounds.

Analysis-generated early exit candidates are filtered by a rejection ratio threshold. The default is `EarlyExitConfig.MinRejectionRatio = 0.05`, meaning a candidate must reject at least 5% of the measured keyspace to be kept. Numeric candidates are measured against the analyzed numeric range. String candidates are measured against the observed length span, first-unit span, or last-unit span, depending on the exit.

Only the best candidates are kept. The default is `EarlyExitConfig.MaxCandidates = 4`, sorted by rejected keyspace size. Candidates can also be disabled globally, per structure, per early-exit type, or by type-specific density limits. The defaults only allow value bitmasks with density up to `0.25`, and length/character bitmaps with density up to `0.45`. Numeric analysis is also skipped when the item count is less than or equal to `EarlyExitConfig.MinItemCount`; string analysis does not use that item-count gate.

After mandatory and analysis-generated exits are combined, FastData removes duplicate exits and, when expression optimization is enabled, removes weaker overlapping bounds. For example, `Length(input) < 3` is removed if `Length(input) < 5` is also present.

### Numeric early exits
#### Value less than early exit
Rejects keys that are smaller than the minimum observed value. Input: `4 9 42 99 123` yields `if (value < 4) return false;`.

#### Value greater than early exit
Rejects keys that are larger than the maximum observed value. Input: `4 9 42 99 123` yields `if (value > 123) return false;`.

#### Value in range early exit
Rejects gaps between observed ranges by checking if the input is strictly inside a missing interval. Input: `10 12 20` yields `if (value > 12 && value < 20) return false;`.

#### Value bitmask early exit
For integral keys, builds a mask from bits that are never set by any observed key and rejects any key that sets one of them. This is only emitted when the mask is useful and passes the density limit. Input: `2 4 6 8` yields `if ((value & 1) != 0) return false;`.

`ValueNotEqualEarlyExit` and `ValueBitSetEarlyExit` are implemented expression types, but they are not currently produced by the numeric early-exit analyzer.

### String early exits
#### Generator function contract
Generated string early exits are expressed through a small cross-language helper API:

- `UnitAt(value, offset)`
- `UnitAtAsciiLower(value, offset)`
- `Length(value)`
- `EqualsAt(value, offset, fragment)`
- `EqualsAtAsciiLower(value, offset, fragment)`

`Unit` means the selected addressable string unit for the generated target. For byte-oriented targets it is a byte. For UTF-16 code-unit targets it is a UTF-16 code unit.

Offsets are compile-time constants in generated code. `offset >= 0` indexes from the start, and `offset < 0` indexes from the end, with `-1` meaning the last unit. Helper implementations may keep this as a branch in source code because optimized builds should inline the helper and remove the branch for constant offsets.

`AsciiLower` means ASCII-only normalization of `A-Z` to `a-z`. It is not Unicode case folding and it is not culture-sensitive. Implementations should use branch-light ASCII lowering, such as `candidate = unit | 0x20` followed by an unsigned ASCII range check.

Because `AsciiLower` is intentionally ASCII-only, FastData does not emit unit or fixed-position string early exits for case-insensitive non-ASCII string datasets. Those lookups still use length exits and the final ordinal-ignore-case equality logic.

`EqualsAt` and `EqualsAtAsciiLower` are fixed-position region comparisons used for prefix, suffix, or fragment checks.

#### Length less than early exit
Rejects strings shorter than the minimum observed length. Input: `horse pig cow sheep` yields `if (value.Length < 3) return false;`.

#### Length greater than early exit
Rejects strings longer than the maximum observed length. Input: `horse pig cow sheep` yields `if (value.Length > 5) return false;`.

#### Length not equal early exit
Used when all lengths are identical. Input: `cow pig cat` yields `if (value.Length != 3) return false;`.

#### String length range early exit
Rejects gaps between observed length ranges by checking for missing length intervals. Input lengths `3 4 7` yield `if (value.Length > 4 && value.Length < 7) return false;`.

#### Length bitmap early exit
Builds a 64 bit bitmap of observed lengths and rejects missing lengths in the 1 to 64 range. This is only emitted when the observed length density passes the density limit. Input: `stable softice sophisticated santa` yields `if ((4208UL & (1UL << ((value.Length - 1) & 63))) == 0) return false;`.

#### Unit at offset less than early exit
Rejects strings whose selected unit is below the minimum observed unit at that offset. Offset `0` is the first unit. Offset `-1` is the last unit. Input: `cat dog emu` yields `if (UnitAt(value, 0) < 'c') return false;`.

#### Unit at offset greater than early exit
Rejects strings whose selected unit is above the maximum observed unit at that offset. Input: `cat dog emu` yields `if (UnitAt(value, 0) > 'e') return false;`.

#### Unit at offset not equal early exit
Used when all strings share the same selected unit. Input: `apple axe ant` yields `if (UnitAt(value, 0) != 'a') return false;`. When ignore-case is enabled this uses `UnitAtAsciiLower`.

#### Unit at offset bitmap early exit
Builds a bitmap of observed selected units and rejects missing ASCII units. This is only emitted when the observed unit density passes the density limit. Input: `alpha zulu` yields a bitmap test for `UnitAt(value, 0)`. When ignore-case is enabled this uses `UnitAtAsciiLower`.

#### Equals at offset early exit
Rejects strings that do not contain an observed fragment at a fixed offset. Offset `0` acts as a prefix check, and a negative offset acts as a suffix check. Input: `preOne preTwo preSix` yields `if (!EqualsAt(value, 0, "pre")) return false;`. Input: `OneSuf TwoSuf SixSuf` yields `if (!EqualsAt(value, -3, "Suf")) return false;`. When ignore-case is enabled this uses `EqualsAtAsciiLower`.

## Structure specializations

### Single value

When the dataset has exactly one unique key, FastData emits a direct equality check and optional value return. This removes all indexing, hashing, and table metadata.

### Range

For numeric membership datasets with compact consecutive ranges, FastData stores start/end pairs instead of every key. This is especially effective for dense clusters separated by gaps.

### BitSet

Dense integral datasets can be represented by one bit per possible value in the observed range. Lookup becomes a range check plus a bit test.

### Bloom filter

When approximate matching is enabled, FastData can use a Bloom filter for membership checks. This trades exactness for compact memory and constant-time rejection.

### RRR bit vector and Elias-Fano

Very sparse integral membership datasets can use succinct encodings. `RrrBitVectorStructure` compresses bit-vector blocks into classes and offsets. `EliasFanoStructure` splits sorted values into upper and lower bit streams with samples for faster navigation.

### Hash table

#### Small hash table type optimization
Usually hash table implementations needs to store some infrastructure to perform its job correctly. However, it adds
quite a lot of memory overhead. FastData detects when the hash table is small enough for using smaller types, thereby saving some memory.

#### Keys are floating point numbers, but with no special values
Floating point numbers have the concept of Not a Number (NaN) as well as multiple binary representations of zero.
Because of this, a good float hash function will fold the many representations into a single representation. This ensures correctness.

However, the check adds overhead. When FastData does not see Zero or NaN in the dataset, it uses a faster hash function.

#### Keys are identity hashed
When the input is integer based, FastData uses an identity hash function (a hash of the key is the key itself).
Because of that, we don't need to store both the key and the hash of the key. It saves 8 bytes pr. key.

#### No collision on keys
If the keys have no collisions among them, a special data structure called PerfectHashTable is produced.
It is like a normal HashTable, but without any logic for collision resolution, thereby making it faster and saving up to 4 bytes pr. key.

#### Compact hash table layout
When the generated table does not need all metadata used by the general hash table, FastData can use a compact hash table layout. This reduces per-entry storage while keeping constant-time lookup behavior.

## Hash function optimizations
- For integer hashes, FastData uses identity hashes. That means the value itself is used as a hash.
- For float/double hashes, FastData uses a NaN/Zero aware hash function that otherwise just use the binary value as the hash.

When it comes to strings, FastData uses a novel technique for determining the best hash function.

By default, it uses a generic hash function. It has decent mixing qualities (based on DJB2), but it is not as good as more modern hash functions.
When it comes to hashing, we need a function that mixes thoroughly, but it also needs to be fast. Unfortunately, more mixing usually means less speed.

The challenge becomes finding a hash function that mixes the dataset really well, but with as few instructions as possible. FastData uses 3 different methods:

- **Brute force**: Only very simple constructs are made, but sometimes a naive search will yield a high performance hash function with excellent mixing qualities.
- **GPerf**: GPerf is a clever way of finding unique positions in strings that gives the best mixing, and then uses a fast advanced algorithm for finding good hash functions. FastData follows the upstream byte-oriented algorithm options for key positions, 7-bit validation, length contribution, association-value sizing, jumps, randomness, and multiple iterations. Duplicate keyword signatures remain unsupported.
- **Genetics**: It has a fitness function that is measured in time and mixing quality. FastData starts with a set of random hash functions, then measure them, and then let them compete in a tournament style competition. Once a good candidate is found, it is further mutated, refined and put into a new tournament. This yields functions that are tailored to the dataset with hopefully good qualities.

These three methods each yield hash function candidates, which are tested on the actual dataset, and the best function wins.
If there is a perfect hash function among the candidates, it is preferred over a non-perfect one. This is because the resulting data structure can be simplified, if we can guarantee a perfect hash function.