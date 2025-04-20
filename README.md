# FastData

[![NuGet](https://img.shields.io/nuget/v/Genbox.ReplaceMe.svg?style=flat-square&label=nuget)](https://www.nuget.org/packages/Genbox.ReplaceMe/)
[![License](https://img.shields.io/github/license/Genbox/ReplaceMe)](https://github.com/Genbox/ReplaceMe/blob/master/LICENSE.txt)

## Description

FastData is a source generator that analyzes your data and creates high-performance, read-only lookup data structures for static data.

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

## Features

- **Data Analysis:** Optimizes the structure based on the inherent properties of the dataset.
- **Multiple Indexing Structures:** Choose the best structure for your data size and use case:
    - Array (linear search)
    - Array (binary search)
    - Array (Eytzinger search)
    - Length indexed
    - Minimal Perfect Hashing
    - Hash tables/sets

It supports several output programming languages:

* C#
* C++

## Getting started

There are several ways of running FastData. See the sections below for details.

### Using the executable

1. Download the FastData executable
2. Create a file with an item pr. line
3. Run `FastData csharp File.txt`

### Using it in a C# application

1. Add the `Genbox.FastData.Generator.CSharp` package to your project
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

### Using it as a .NET Source Generator

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

Whenever you change the array, it automatically generates the new source code and includes in your project.

## How does it work?

The idea behind the project is to generate a data-dependent optimized data structure for read-only lookup. When data is known beforehand, the algorithm can select from a set
of different data structures, indexing, and comparison methods that are tailor built for the data.

### Compile-time generation

There are many benefits gained from generating data structures at compile time:

* Enables us to analyze the data
* Zero runtime overhead
* No defensive copying of data (takes time and needs double the memory)
* No virtual dispatching (Virtual method calls & inheritance)
* Data as code means you can compile the data into your assembly

### Data analysis

FastData uses advanced data analysis techniques to generate optimized data structures. Analysis consists of:

* Length bitmaps
* Entropy mapping
* Character mapping
* Encoding analysis

#### Hash function generators

Hash functions come in many flavors. Some are designed for low latency, some for throughput, others for low collision rate.
Programming language runtimes come with a hash function that is a tradeoff between these parameters. FastData builds a hash function specifically tailored to the dataset.
It does so using one of four techniques:

1. **Default:** If no technique is selected, FastData uses a hash function by Daniel Bernstein (DJB2)
2. **Brute force:** It spends some time on trying increasingly stronger hash functions
3. **Heuristic:** It tries to build a hash function that selects for entropy in strings
4. **Genetic algorithm:** It uses machine learning to evolve a hash function from scratch that matches the data effectively

## Best practices

* Put the most often queried items first in the input data. It can speed up query speed for some data structures.

## FAQ

* Q: Why not use System.Collections.Frozen?
* A: There are several reasons:
    * Frozen comes with considerable runtime overhead
    * Frozen is only available in .NET 8.0+
    * Frozen only provides af ew of the optimizations provided in FastData