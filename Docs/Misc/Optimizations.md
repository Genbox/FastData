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