# Data structures

By default, FastData chooses the optimal data structure for your data, but you can also set it manually with
`FastData -s <type>`.

Below you'll see details of each structure.

- **Memory:** The memory overhead of the data structure. _Native_ means it does not have any overhead.
- **Complexity:** The data structures complexity expressed in [Big O notation](https://en.wikipedia.org/wiki/Big_O_notation).

## Array

### Overview

* Memory: Native
* Complexity: O(n)

This data structure uses an array as the backing store. It is faster than a normal array due to early exits (value/length range checks).
It works well for small amounts of data, but for larger datasets, the linear scans on data hurts performance.

## BinarySearch

* Memory: Native
* Complexity: O(log n)

This data structure sorts your data and does a binary search on it. Since data is sorted at compile time, there is no
overhead at runtime. It is good for medium sized datasets when memory usage is a concern.

## Conditional

* Memory: Native
* Complexity: O(n)

This data structure relies on built-in logic in the programming language. It produces logic statements which
ultimately become machine instructions. It is faster than an array for small amounts of data, but as soon as there are more than 400-500 keys, it starts declining.

## HashTable

* Memory: up to 16 bytes pr. key
* Complexity: O(1)

This data structure is based on a hash table with separate chaining collision resolution. It uses a separate array for
buckets to stay cache coherent, but it also uses more
memory since it needs to keep track of indices.

### Special cases

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

## Comparison
We can compare each data structure in a graph and see which one is the fastest for a given number of keys.
The Y-axis is the number of queries per second (QPS). The X-axis is the number of keys.

![StructuresGraph.png](StructuresGraph.png)

We can see that Conditional starts out by being the fastest, but at ~500 keys, it declines sharply.
HashTable has stable lookup performance no matter how many keys it contains, but for less than 10 items, it is not a good choice.