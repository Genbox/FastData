# FastData

[![License](https://img.shields.io/github/license/Genbox/FastData)](https://github.com/Genbox/FastData/blob/master/LICENSE.txt)

![Docs/FastData.png](Docs/FastData.png)

## Description

FastData is a code generator that analyzes your data and creates high-performance, read-only lookup data structures for
static data. It can output the data structures
in many different languages (C#, C++, Rust, etc.), ready for inclusion in your project with zero dependencies.

## Download

[![C# library](https://img.shields.io/nuget/v/Genbox.FastData.Generator.CSharp.svg?style=flat-square&label=nuget)](https://www.nuget.org/packages/Genbox.FastData.Generator.CSharp/)
[![.NET Tool](https://img.shields.io/nuget/v/Genbox.FastData.Cli.svg?style=flat-square&label=nuget)](https://www.nuget.org/packages/Genbox.FastData.Cli/)


## Use case

Imagine a scenario where you have a predefined list of words (e.g., dog breeds) and need to check whether a specific dog
breed exists in the set.
Usually you create an array and look up the value. However, this is far from optimal and is lacks several optimizations.

```csharp
string[] breeds = ["Labrador", "German Shepherd", "Golden Retriever"];

if (breeds.Contains("Beagle"))
    Console.WriteLine("It contains Beagle");
```

We can do better by analyzing the dataset and generating an optimized data structure.

1. Create a file `Dogs.txt` with the following contents:

```
Labrador
German Shepherd
Golden Retriever
```

2. Run `FastData csharp Dogs.txt`. It produces the following output:

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
- **Efficient Lookups:** A switch-based data structure which is fast for small datasets.
- **Additional Metadata:** Provides item count and other useful properties.

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

### Using it as a C# library

1. Add the `Genbox.FastData.Generator.CSharp` NuGet package to your project.
2. Use the `FastDataGenerator.TryGenerate()` method. Give it your data as an array.

```csharp
internal static class Program
{
    private static void Main()
    {
        FastDataConfig config = new FastDataConfig();

        CSharpCodeGenerator generator = new CSharpCodeGenerator(new CSharpGeneratorConfig("Dogs"));

        if (!FastDataGenerator.TryGenerate(["Labrador", "German Shepherd", "Golden Retriever"], config, generator, out string? source))
            Console.WriteLine("Failed to generate source code");

        Console.WriteLine(source);
    }
}
```

Whenever you change the array, it automatically generates the new source code.

## Features

- **Data Analysis:** Optimizes the structure based on the inherent properties of the dataset.
- **Multiple Structures:** FastData automatically chooses the best data structure for your data.
- **Fast hashing:** String lookups are fast due to a fast string hash function

It supports several output programming languages.

* C#: `FastData csharp <input-file>`
* C++: `FastData cplusplus <input-file>`
* Rust: `FastData rust <input-file>`

Each output language has different settings. Type `FastData <lang> --help` to see the options.

### Data structures

By default, FastData chooses the optimal data structure for your data, but you can also set it manually with
`FastData -s <type>`. See the details of each structure type below.

#### Auto
This is the default option. It autoselects the best data structure based on the number of items you provide.

#### Array

* Memory: Low
* Latency: Low
* Complexity: O(n)

This data structure uses an array as the backing store. It is often faster than a normal array due to efficient early
exits (value/length range checks).
It works well for small amounts of data since the array is scanned linearly, but for larger datasets, the O(n)
complexity hurts performance a lot.

#### BinarySearch

* Memory: Low
* Latency: Medium
* Complexity: O(log n)

This data structure sorts your data and does a binary search on it. Since data is sorted at compile time, there is no
overhead at runtime. Each lookup
has a higher latency than a simple array, but once the dataset gets to a few hundred items, it beats the array due to a
lower complexity.

#### Conditional

* Memory: Low
* Latency: Low
* Complexity: O(n)

This data structure relies on built-in logic in the programming language. It produces if/switch statements which
ultimately become machine instructions on the CPU, rather than data
that resides in memory.
Latency is therefore incredibly low, but the higher number of instructions bloat the assembly, and at a certain point it
becomes more efficient to have
the data reside in memory.

#### HashSet

* Memory: Medium
* Latency: Medium
* Complexity: O(1)

This data structure is based on a hash table with separate chaining collision resolution. It uses a separate array for
buckets to stay cache coherent, but it also uses more
memory since it needs to keep track of indices.

## How does it work?

The idea behind the project is to generate a data-dependent optimized data structure for read-only lookup. When data is
known beforehand, the algorithm can select from a set
of different data structures, indexing, and comparison methods that are tailor-built for the data.

### Compile-time generation

There are many benefits gained from generating data structures at compile time:

* Enables otherwise time-consuming data analysis
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

It uses the analysis to create so-called early-exits, which are fast `O(1)` checks on your input before doing any `O(n)`
checks on the actual dataset.

## Best practices

* Put the most often queried items first in the input data. It can speed up query speed for some data structures.

## FAQ

* Q: Why not use System.Collections.Frozen?
* A: There are several reasons:
    * Frozen comes with considerable runtime overhead
    * Frozen is only available in .NET 8.0+
    * Frozen only provides a few of the optimizations provided in FastData
    * Frozen is only available in C#. FastData can produce data structures in many languages.