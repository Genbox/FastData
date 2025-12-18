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

This data structure uses an array as the backing store. It is faster than _just_ an array due to early exits.
It works well for small amounts of data, but for larger datasets, linear scans hurt performance.

## BinarySearch

* Memory: Native
* Complexity: O(log n)

This data structure sorts your data and does a binary search on it. Since data is sorted at compile time, there is no
overhead at runtime. It is good for medium-sized datasets when memory usage is a concern.

## Conditional

* Memory: Native
* Complexity: O(n)

This data structure relies on built-in logic in the programming language. It produces logic statements which
ultimately become machine instructions. It is faster than an array for small amounts of data, but as soon as there are more than 400-500 keys, it starts declining.

## HashTable

* Memory: up to 16 bytes per key
* Complexity: O(1)

This data structure is based on a hash table with separate chaining collision resolution. It uses a separate array for
buckets and items to stay cache efficient. It outperforms other data structures when the data set becomes large enough.

# Comparison
The graph below shows which data structure is the fastest for a given number of keys.
The Y-axis is the number of queries per second (QPS). The X-axis is the number of keys.

![StructuresGraph.png](StructuresGraph.png)

We can see that Conditional starts out by being the fastest, but at ~500 keys, it declines sharply.
HashTable has stable lookup performance no matter how many keys it contains, but for less than 10 items, it is not a great choice.