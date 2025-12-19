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
However, if we can determine the input set is contiguous, then we don't need to do the binary search at all.

Let's say the input is 42, 43, 44, 45.
- A check if 100 is in the list, is just `is 100 >= 45?`
- A check if 43 is in the list, is just `is 43 >= 42 and <= 45?`

We reduce the complexity of binary search from logarithmic to constant lookups.

### Reduction to length lookup
When indexing strings, you have to work with a hash function to make the string into an integer. That can be a pretty heavy operation that will dominate the time it takes to lookup an element.
Can we do better? Let's say you have an input like this: `house car fish`

In this case, each of the input strings have a unique length. So why not use their length as their hash? That's exactly what `KeyLengthStructure` does.
If the programming language cache the string length, we can get constant lookup time.

## Early exits
An early exit is a lightweight check we can perform before the actual lookup to speed up the process. They have to be **really** fast, otherwise they will add unwanted overhead to each call.
Only generalized data structures perform early exits, as reductions already have early exits built into them.

### Value range early exit
Let's say we have a hash table data structure. Each lookup needs to hash the input data, lookup the bucket and do an equality check. With just one additional check, we can say that an element won't be in dataset at all.
Input: `4 9 42 99 123`

If we track the minimum and maximum values, instead of doing a lookup, we can just check if the range matches: `if (value < 4 || value > 123) return false;`

### Length range early exit
This one works much like the other early exit above, but checks the length of the value, rather than the value itself. Therefore it only works on arrays (strings are interpreted as an array in that context).
Input: `horse pig cow sheep`

We track the minimum and maximum lengths, and can do a check like this: `if (value.Length < 3 || value.Length > 5) return false;`

There is also another variant that checks if all lengths are the same. In that case, we can save an if-statement.
Input `cow pig cat`

It will produce: `if (value.Length != 3) return false;`

### Length bitmap early exit
FastData can also sometimes produce a bitmap of value lengths. It basically map lengths into a 64bit integer and check if the corresponding bit is set.
Input: `stable softice sophisticated santa`

It generates this early exit: `if ((4208UL & (1UL << (value.Length - 1))) == 0) return false;`

## Structure specializations

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