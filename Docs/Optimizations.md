# Optimizations
FastData contains a lot of optimizations that are not that obvious from the get-go. I'll try and document them here.

## Reductions
A reduction is the process of going from a generalized data structure, to a specialized data structure that is much faster and efficient.

### Reduction to single element lookup
If you create a hash table with a single element in any programming language, the hash table still have to hash inputs, match it to a bucket and do equality.
But since there is just one element, we don't have to do all that. FastData will return a specialized data structure that just check against the single element.

### Reduction to range lookup
This trick pertains to all types of lookup structures, but I'll use binary search as the example.

Binary search works by segmenting an array in half on each iteration of a lookup. It works on any input data, as long as we can establish a concept of `greater than`, `equal` and `less than` between elements.
However, if we can determine the input set is consecutive, then we don't need to do the binary search at all.

Let's say the input is 42, 43, 44, 45.
- A check if 100 is in the list, is just `is 100 >= 45?`
- A check if 43 is in the list, is just `is 43 >= 42 and <= 45?`

We reduce the complexity of binary search from logarithmic to constant lookups.

### Reduction to length lookup
When indexing strings, you have to work with a hash function to make the string into an integer. That can be a pretty heavy operation that will dominate the time it takes to lookup an element.
Can we do better? Let's say you have an input like this: `house car fish`

In this case, each of the input strings have a unique length. So why not use their length as their hash? That's exactly what `KeyLengthStructure` does.
If the programming language cache the string length, we can get constant lookup time.

### Reduction to bitmap lookup
If a sequence of numbers is a short range, but cannot get optimized via the range reduction above, then it might be a good fit for a bitmap instead. Let's say you have the numbers in range 10-100 and 300-900.
Range reduction works when the numbers can be represented as a compact set of ranges, and there are too many numbers to use conditionals efficiently, so instead we use a bitmap when the range is dense enough.

## Early exits
An early exit is a lightweight check we can perform before the actual lookup to speed up the process. They have to be **really** fast, otherwise they will add unwanted overhead to each call.
Only generalized data structures perform early exits, as reductions already have early exits built into them.

Early exit candidates are filtered by a rejection ratio threshold. The default is `EarlyExitConfig.MinRejectionRatio = 0.05`, meaning a candidate must reject at least 5% of the observed range to be kept.

### Numeric early exits
#### Value less than early exit
Rejects keys that are smaller than the minimum observed value. Input: `4 9 42 99 123` yields `if (value < 4) return false;`.

#### Value greater than early exit
Rejects keys that are larger than the maximum observed value. Input: `4 9 42 99 123` yields `if (value > 123) return false;`.

#### Value not equal early exit
Used when all values are identical. It emits `if (value != 42) return false;` for input `42 42 42`.

#### Value in range early exit
Rejects gaps between observed ranges by checking if the input is strictly inside a missing interval. Input: `10 12 20` yields `if (value > 12 && value < 20) return false;`.

#### Value bitmask early exit
Builds a bitmask of bits never seen in the dataset and rejects any key that sets one of them. Input: `2 4 6 8` yields `if ((key & 1) != 0) return false;`.

#### Value bitset early exit
Builds a bitmap of missing values inside a compact range and rejects any key that lands on a missing value. Input: `10 12 13` yields a range check plus a missing bitset check.

### String early exits
#### Length less than early exit
Rejects strings shorter than the minimum observed length. Input: `horse pig cow sheep` yields `if (value.Length < 3) return false;`.

#### Length greater than early exit
Rejects strings longer than the maximum observed length. Input: `horse pig cow sheep` yields `if (value.Length > 5) return false;`.

#### Length not equal early exit
Used when all lengths are identical. Input: `cow pig cat` yields `if (value.Length != 3) return false;`.

#### String length range early exit
Rejects gaps between observed length ranges by checking for missing length intervals. Input lengths `3 4 7` yield `if (value.Length > 4 && value.Length < 7) return false;`.

#### Length bitmap early exit
Builds a 64 bit bitmap of observed lengths and rejects missing lengths in the 1 to 64 range. Input: `stable softice sophisticated santa` yields `if ((4208UL & (1UL << (value.Length - 1))) == 0) return false;`.

#### Char first less than early exit
Rejects strings whose first ASCII character is below the minimum observed first character. Input: `cat dog emu` yields `if (first < 'c') return false;`.

#### Char first greater than early exit
Rejects strings whose first ASCII character is above the maximum observed first character. Input: `cat dog emu` yields `if (first > 'e') return false;`.

#### Char first not equal early exit
Used when all strings share the same first character. Input: `apple axe ant` yields `if (first != 'a') return false;`.

#### Char first bitmap early exit
Builds a bitmap of observed first ASCII characters and rejects missing characters. Input: `alpha zulu` yields a bitmap test for the first character.

#### Char last less than early exit
Rejects strings whose last ASCII character is below the minimum observed last character. Input: `alpha bravo charlie` yields `if (last < 'a') return false;`.

#### Char last greater than early exit
Rejects strings whose last ASCII character is above the maximum observed last character. Input: `alpha bravo charlie` yields `if (last > 'e') return false;`.

#### Char last not equal early exit
Used when all strings share the same last character. Input: `tuba panda villa` yields `if (last != 'a') return false;`.

#### Char last bitmap early exit
Builds a bitmap of observed last ASCII characters and rejects missing characters. Input: `alpha zulu` yields a bitmap test for the last character.

#### String prefix early exit
Rejects strings that do not start with an observed prefix when trimming is disabled. Input: `preOne preTwo preSix` yields `if (!StartsWith("pre")) return false;`.

#### String suffix early exit
Rejects strings that do not end with an observed suffix when trimming is disabled. Input: `OneSuf TwoSuf SixSuf` yields `if (!EndsWith("Suf")) return false;`.

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
- **GPerf**: GPerf is a clever way of finding unique positions in strings that gives the best mixing, and then uses an fast advanced algorithm for finding good hash functions.
- **Genetics**: It has a fitness function that is measured in time and mixing quality. FastData starts with a set of random hash functions, then measure them, and then let them compete in a tournament style competition. Once a good candidate is found, it is further mutated, refined and put into a new tournament. This yields functions that are tailored to the dataset with hopefully good qualities.

These three methods each yield hash function candidates, which are tested on the actual dataset, and the best function wins.
If there is a perfect hash function among the candidates, it is preferred over a non-perfect one. This is because the resulting data structure can be simplified, if we can guarantee a perfect hash function.