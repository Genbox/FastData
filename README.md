# FastData

[![License](https://img.shields.io/github/license/Genbox/FastData)](https://github.com/Genbox/FastData/blob/master/LICENSE.txt)

![Docs/FastData.png](./Docs/FastData.png)

## Description

FastData is a code generator that analyzes your data and creates high-performance, read-only data structures with key/value and membership queries on
static data. It supports many different languages (C#, C++, Rust, etc.), ready for inclusion in your project with zero dependencies.

## Download

| Name                | Link                                                                                                                                                                                        |
|---------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Executable          | [GitHub Releases](https://github.com/Genbox/FastData/releases/latest)                                                                                                                       |
| C# source generator | [![C# source generator](https://img.shields.io/nuget/v/Genbox.FastData.SourceGenerator.svg?style=flat-square&label=nuget)](https://www.nuget.org/packages/Genbox.FastData.SourceGenerator/) |
| C# library          | [![C# library](https://img.shields.io/nuget/v/Genbox.FastData.Generator.CSharp.svg?style=flat-square&label=nuget)](https://www.nuget.org/packages/Genbox.FastData.Generator.CSharp/)        |
| .NET Tool           | [![.NET Tool](https://img.shields.io/nuget/v/Genbox.FastData.Cli.svg?style=flat-square&label=nuget)](https://www.nuget.org/packages/Genbox.FastData.Cli/)                                   |
| PowerShell          | [![PowerShell Gallery](https://img.shields.io/powershellgallery/v/Genbox.FastData.svg?style=flat-square&label=powershell)](https://www.powershellgallery.com/packages/Genbox.FastData/)     |

## Getting started

For some guides below you'll need a `dogs.txt`file with the following contents:

```
Labrador
German Shepherd
Golden Retriever
```

### Using the executable

1. [Download](https://github.com/Genbox/FastData/releases/latest) the executable
2. Run `FastData csharp dogs.txt`

### Using the .NET CLI tool
1. install the [Genbox.FastData.Cli tool](https://www.nuget.org/packages/Genbox.FastData.Cli/): `dotnet tool install --global Genbox.FastData.Cli`
2. Run `FastData csharp dogs.txt`

### Using the PowerShell module
1. Install the [PowerShell module](https://www.powershellgallery.com/packages/Genbox.FastData/): `Install-Module -Name Genbox.FastData`
2. Run `Invoke-FastData -Language CSharp -InputFile dogs.txt`

### Using the .NET Source Generator

1. Add the [Genbox.FastData.SourceGenerator](https://www.nuget.org/packages/Genbox.FastData.SourceGenerator/) package to your project
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

The `Dogs` class is generated at compile time and contains a `Contains()` method that checks if the value exists in the dataset.

### Using the C# library

1. Add the [Genbox.FastData.Generator.CSharp](https://www.nuget.org/packages/Genbox.FastData.Generator.CSharp/) NuGet package to your project.
2. Use the `FastDataGenerator.Generate()` method. Give it your data as an array.

```csharp
internal static class Program
{
    private static void Main()
    {
        FastDataConfig config = new FastDataConfig();
        CSharpCodeGenerator generator = CSharpCodeGenerator.Create(new CSharpCodeGeneratorConfig("Dogs"));

        string source = FastDataGenerator.Generate(["Labrador", "German Shepherd", "Golden Retriever"], config, generator);
        Console.WriteLine(source);
    }
}
```

## Why use FastData?

Imagine a scenario where you have a predefined list of words (e.g., dog breeds) and need to check whether a specific dog breed exists in the set.
Usually you create an array and look up the value. However, this is far from optimal and lacks several optimizations.

```csharp
string[] dogs = { "Labrador", "German Shepherd", "Golden Retriever" };

if (dogs.Contains("Beagle"))
    Console.WriteLine("It contains Beagle");
```

We can do better by analyzing the dataset and generating an optimized data structure.
Running FastData on the same dataset produces the following code:

```csharp
internal static class Dogs
{
    public static bool Contains(string value)
    {
        if ((49280UL & (1UL << (value.Length - 1))) == 0)
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

- **Fast early exit:** A bitmap of string lengths allows early termination for string lengths that cannot be in the set.
- **Efficient lookups:** A switch-based data structure which is fast for small datasets.
- **Additional metadata:** Provides item count and other useful properties.

A benchmark of the array versus our generated structure really illustrates the difference. It is 13x faster.

| Method   |      Mean |     Error |    StdDev | Ratio |
|----------|----------:|----------:|----------:|------:|
| FastData | 0.5311 ns | 0.0170 ns | 0.0026 ns |  0.08 |
| Array    | 6.9286 ns | 0.1541 ns | 0.0684 ns |  1.00 |

## Features

- **Data analysis:** Optimizes the structure based on the inherent properties of the dataset.
- **Multiple structures:** FastData automatically chooses the best data structure for your data.
- **Fast hashing:** String lookups are fast due to a fast string hash function
- **Zero dependencies:** The generated code has no dependencies, making it easy to integrate into your project.
- **Minimal memory usage:** The generated data structures are memory-efficient, using only the necessary amount of memory for the dataset.
- **High-perfromance:** The generated data structures are generated without unnecessary branching or virtualization making the compiler produce optimal code.

It supports several output programming languages.

* C#: `FastData csharp <input-file>`
* C++: `FastData cplusplus <input-file>`
* Rust: `FastData rust <input-file>`

Each output language has different settings. Type `FastData <lang> --help` to see the options.

### Data structures

By default, FastData chooses the optimal data structure for your data, but you can also set it manually with
`FastData -s <type>`. See the details of each structure type below.

#### Auto
This is the default option. It automatically selects the best data structure based on the number of items you provide.

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

* _Data as code_ means you can compile the data into your assembly
* Enables otherwise time-consuming data analysis (e.g. zero runtime overhead)
* No defensive copying of data (takes time and needs double the memory)
* No virtual dispatching (virtual method calls & inheritance) and no unnecessary branching
* Modulo operations are known constants and compilers optimize it to bitwise operations
* Data can be stored in smaller data types (e.g. `byte` instead of `int`) in values permit it
* Data can be encoding reduced. That is, if all characters are ASCII, they can be stored as single bytes, which saves memory and improves performance.

### Data analysis

FastData uses advanced data analysis techniques to generate optimized data structures. Analysis consists of:

* Length bitmaps
* Entropy mapping
* Character mapping
* Encoding analysis

It uses the analysis to create so-called early-exits, which are fast `O(1)` checks on your input before doing any `O(n)`
checks on the actual dataset.

## FAQ

#### Why not use System.Collections.Frozen?
There are several reasons:
* Frozen comes with considerable runtime overhead
* Frozen is only available in .NET 8.0+
* Frozen only provides a few of the optimizations provided in FastData
* Frozen is only available in C#. FastData can produce data structures in many languages.

#### Does it support case-insensitive lookups?
No, not yet.

#### Does it support custom equality comparers?
No, not yet.

#### Does it support keyed lookup?
No, not yet.

#### Are there any best pratcies for using FastData?
Yes. See below:
* Put the most often queried items first in the input data. It can speed up query speed for some data structures.

#### Can I use it for dynamic data?
No, FastData is designed for static data only. It generates code at compile time, so the data must be known beforehand.