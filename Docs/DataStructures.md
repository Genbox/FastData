# Data structures

By default, FastData chooses the optimal data structure for your data, but you can also set it manually with
`FastData -s <type>`. See the details of each structure type below.

## Auto
This is the default option. It automatically selects the best data structure based on the number of items you provide.

## Array

* Memory: Low
* Latency: Low
* Complexity: O(n)

This data structure uses an array as the backing store. It is often faster than a normal array due to efficient early
exits (value/length range checks).
It works well for small amounts of data since the array is scanned linearly, but for larger datasets, the O(n)
complexity hurts performance a lot.

## BinarySearch

* Memory: Low
* Latency: Medium
* Complexity: O(log n)

This data structure sorts your data and does a binary search on it. Since data is sorted at compile time, there is no
overhead at runtime. Each lookup
has a higher latency than a simple array, but once the dataset gets to a few hundred items, it beats the array due to a
lower complexity.

## Conditional

* Memory: Low
* Latency: Low
* Complexity: O(n)

This data structure relies on built-in logic in the programming language. It produces if/switch statements which
ultimately become machine instructions on the CPU, rather than data
that resides in memory.
Latency is therefore incredibly low, but the higher number of instructions bloat the assembly, and at a certain point it
becomes more efficient to have
the data reside in memory.

## HashSet

* Memory: Medium
* Latency: Medium
* Complexity: O(1)

This data structure is based on a hash table with separate chaining collision resolution. It uses a separate array for
buckets to stay cache coherent, but it also uses more
memory since it needs to keep track of indices.