## How does it work?

The idea behind the project is to generate a data-dependent optimized data structure for read-only lookup. When data is
known beforehand, the algorithm can select from a set
of different data structures, indexing, and comparison methods that are tailor-built for the data.

### Compile-time generation

There are many benefits gained from generating data structures at compile time:

* _Data as code_ means you can compile the data into your assembly
* Enables otherwise time-consuming data analysis (e.g. zero runtime overhead)
* No defensive copying of data (takes time and needs double the memory)
* No virtual dispatching (virtual method calls & inheritance) and no unnecessary branching
* Modulo operations are known constants and compilers optimize it to bitwise operations
* Data can be stored in smaller data types (e.g. `byte` instead of `int`) if values permit it
* Data can be encoding reduced. That is, if all characters are ASCII, they can be stored as single bytes, which saves memory and improves performance.

### Data analysis

FastData uses advanced data analysis techniques to generate optimized data structures. Analysis consists of:

* Length bitmaps
* Entropy mapping
* Character mapping
* Encoding analysis

It uses the analysis to create early-exits, which are fast checks on input values before doing any lookups on the actual dataset.