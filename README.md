# FastData

[![NuGet](https://img.shields.io/nuget/v/Genbox.FastData.svg?style=flat-square&label=nuget)](https://www.nuget.org/packages/Genbox.FastData/)
[![License](https://img.shields.io/github/license/Genbox/FastData)](https://github.com/Genbox/FastData/blob/master/LICENSE.txt)

## Description

FastData is a code generator that analyzes your data and creates high-performance, read-only lookup data structures for static data. It can output the data structures
in many different languages (C#, C++, Rust, etc.), ready for inclusion in your project with zero dependencies.

## Use case

Imagine a scenario where you have a predefined list of words (e.g., dog breeds) and need to check whether a specific dog breed exists in the set.
Usually you create an array and look up the value. However, this is far from optimal and is missing a few optimizations.

```csharp
string[] breeds = ["Labrador", "German Shepherd", "Golden Retriever"];

if (breeds.Contains("Beagle"))
    Console.WriteLine("It contains Beagle");
```

We can do better by analyzing the dataset and generating a data structure optimized for the data.

1. Create a file `Dogs.txt` with the following contents:

```
Labrador
German Shepherd
Golden Retriever
```

2. Run `FastData csharp Dogs.txt`. It produces the output:

```csharp
internal static class Dogs
{
    public static bool Contains(string value)
    {
        if ((49280UL & (1UL << (value.Length - 1) % 64)) == 0)
           return false;

        switch (value)
        {
            case "Labrador":
            case "German Shepherd":
            case "Golden Retriever":
                return true;
            default:
                return false;
        }
    }

    public const int ItemCount = 3;
    public const int MinLength = 8;
    public const int MaxLength = 16;
}
```

Benefits of the generated code:

- **Fast Early Exit:** A bitmap of string lengths allows early termination for out-of-range values.
- **Efficient Lookups:** A switch-based data structure. It uses more advanced structures for larger data sets.
- **Additional Metadata:** Provides item count and minimum/maximum string length.

A benchmark of the array versus our generated structure really illustrates the difference. It is 13x faster.

| Method    |      Mean |     Error |    StdDev | Ratio |
|-----------|----------:|----------:|----------:|------:|
| Generated | 0.5311 ns | 0.0170 ns | 0.0026 ns |  0.08 |
| Array     | 6.9286 ns | 0.1541 ns | 0.0684 ns |  1.00 |

## Getting started

There are several ways of running FastData. See the sections below for details.

### Using the executable

1. Download the FastData executable
2. Create a file with an item per line
3. Run `FastData csharp File.txt`

### Using it in a C# application

1. Add the `Genbox.FastData.Generator.CSharp` nuget package to your project.
2. Use the `FastDataGenerator.TryGenerate()` method. Give it your data as an array.

```csharp
internal static class Program
{
    private static void Main()
    {
        FastDataConfig config = new FastDataConfig();
        config.StringComparison = StringComparison.OrdinalIgnoreCase;

        CSharpCodeGenerator generator = new CSharpCodeGenerator(new CSharpGeneratorConfig("Dogs"));

        if (!FastDataGenerator.TryGenerate(["Labrador", "German Shepherd", "Golden Retriever"], config, generator, out string? source))
            Console.WriteLine("Failed to generate source code");

        Console.WriteLine(source);
    }
}
```

### Using the .NET Source Generator

1. Add the `Genbox.FastData.SourceGenerator` package to your project
2. Add `FastDataAttribute` as an assembly level attribute.

```csharp
using Genbox.FastData.SourceGenerator;

[assembly: FastData<string>("Dogs", ["Labrador", "German Shepherd", "Golden Retriever"])]

internal static class Program
{
    private static void Main()
    {
        Console.WriteLine(Dogs.Contains("Labrador"));
        Console.WriteLine(Dogs.Contains("Beagle"));
    }
}
```

Whenever you change the array, it automatically generates the new source code and includes it in your project.

## Features

- **Data Analysis:** Optimizes the structure based on the inherent properties of the dataset.
- **Multiple Indexing Structures:** FastData automatically chooses the best structure for your data.

It supports several output programming languages.

* C# output: `fastdata csharp <input-file>`
* C++ output: `fastdata cplusplus <input-file>`
* Rust output: `fastdata rust <input-file>`

Each output language has different settings. Type `fastdata <lang> --help` to see the options.

### Data structures

By default, FastData chooses the optimal data structure for your data, but you can also set it manually with `fastdata -s <type>`. See the details of each structure type below.

#### SingleValue

* Memory: Low
* Latency: Low
* Complexity: O(1)

This data structure only supports a single value. It is much faster than an array with a single item and has no overhead associated with it.
FastData always selects this data structure whenever your dataset only contains one item.

#### Conditional

* Memory: Low
* Latency: Low
* Complexity: O(n)

This data structure relies on built-in logic in the programming language. It produces if/switch statements which ultimately become machine instructions on the CPU, rather than data
that resides in memory.
Latency is therefore incredibly low, but the higher number of instructions bloat the assembly, and at a certain point it becomes more efficient to have
the data reside in memory.

#### Array

* Memory: Low
* Latency: Low
* Complexity: O(n)

This data structure uses an array as the backing store. It is often faster than a normal array due to efficient early exits (value/length range checks).
It works well for small amounts of data since the array is scanned linearly, but for larger datasets, the O(n) complexity hurts performance a lot.

#### BinarySearch

* Memory: Low
* Latency: Medium
* Complexity: O(log n)

This data structure sorts your data and does a binary search on it. Since data is sorted at compile time, there is no overhead at runtime. Each lookup
has a higher latency than a simple array, but once the dataset gets to a few hundred items, it beats the array due to a lower complexity.

#### EytzingerSearch

* Memory: Low
* Latency: Medium
* Complexity: O(n*log(n))

This data structure sorts data using an Eytzinger layout. It has better cache-locality than binary search. Under some circumstances it has better performance.

#### KeyLength

* Memory: Low
* Latency: Low
* Complexity: O(1)

This data structure only works on strings, but it indexes them after their length, rather than a hash. In the case all the strings have unique lengths, the
data structure further optimizes for latency.

#### HashSetChain

* Memory: Medium
* Latency: Medium
* Complexity: O(1)

This data structure is based on a hash table with separate chaining collision resolution. It uses a separate array for buckets to stay cache coherent, but it also uses more
memory since it needs to keep track of indices.

#### HashSetLinear

* Memory: Medium
* Latency: Medium
* Complexity: O(1)

This data structure is also a hash table, but with linear collision resolution.

#### PerfectHashBruteForce

* Memory: Low
* Latency: Low
* Complexity: O(1)

This data structure tries to create a perfect hash for the dataset. It does so by brute-forcing a seed for a simple hash function
until it hits the right combination. If the dataset is small enough, it can even produce a minimal perfect hash.

#### PerfectHashGPerf

* Memory: Low
* Latency: Low
* Complexity: O(1)

This data structure uses the same algorithm as gperf to derive a perfect hash. It uses Richard J. Cichelli's method for creating an associative table,
which is augmented using alpha increments to resolve collisions. It only works on strings, but it is great for medium-sized datasets.

## How does it work?

The idea behind the project is to generate a data-dependent optimized data structure for read-only lookup. When data is known beforehand, the algorithm can select from a set
of different data structures, indexing, and comparison methods that are tailor-built for the data.

### Compile-time generation

There are many benefits gained from generating data structures at compile time:

* Enables analysis the data
* Zero runtime overhead
* No defensive copying of data (takes time and needs double the memory)
* No virtual dispatching (virtual method calls & inheritance)
* Data as code means you can compile the data into your assembly

### Data analysis

FastData uses advanced data analysis techniques to generate optimized data structures. Analysis consists of:

* Length bitmaps
* Entropy mapping
* Character mapping
* Encoding analysis

It uses the analysis to create so-called early-exits, which are fast `O(1)` checks on your input before doing any `O(n)` checks on the actual dataset.

#### Hash function generators

Hash functions come in many flavors. Some are designed for low latency, some for throughput, others for low collision rate.
Programming language runtimes come with a hash function that is a tradeoff between these parameters. FastData builds a hash function specifically tailored to the dataset.
It has support for several techniques:

1. **Default:** If no technique is selected, FastData uses a hash function by Daniel Bernstein (DJB2)
2. **Brute force:** It spends some time on trying increasingly stronger hash functions
3. **Heuristic:** It tries to build a hash function that selects for entropy in strings
4. **Genetic algorithm:** It uses machine learning to evolve a hash function that matches the data effectively

## Best practices

* Put the most often queried items first in the input data. It can speed up query speed for some data structures.

## FAQ

* Q: Why not use System.Collections.Frozen?
* A: There are several reasons:
    * Frozen comes with considerable runtime overhead
    * Frozen is only available in .NET 8.0+
    * Frozen only provides a few of the optimizations provided in FastData
    * Frozen is only available in C#. FastData can produce data structures in many langauges.