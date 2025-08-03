# FastData

[![License](https://img.shields.io/github/license/Genbox/FastData)](https://github.com/Genbox/FastData/blob/master/LICENSE.txt)

![Docs/FastData.png](./Docs/FastData.png)

## Description

FastData is a code generator that analyzes your data and creates high-performance, read-only data structures for static data with support for key/value and membership queries.
It supports many output languages (C#, C++, Rust, etc.), ready for inclusion in your project with zero dependencies.

## Download

| Name                | Link                                                                                                                                                                                        |
|---------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Executable          | [GitHub Releases](https://github.com/Genbox/FastData/releases/latest)                                                                                                                       |
| C# source generator | [![C# source generator](https://img.shields.io/nuget/v/Genbox.FastData.SourceGenerator.svg?style=flat-square&label=nuget)](https://www.nuget.org/packages/Genbox.FastData.SourceGenerator/) |
| C# library          | [![C# library](https://img.shields.io/nuget/v/Genbox.FastData.Generator.CSharp.svg?style=flat-square&label=nuget)](https://www.nuget.org/packages/Genbox.FastData.Generator.CSharp/)        |
| .NET Tool           | [![.NET Tool](https://img.shields.io/nuget/v/Genbox.FastData.Cli.svg?style=flat-square&label=nuget)](https://www.nuget.org/packages/Genbox.FastData.Cli/)                                   |
| PowerShell          | [![PowerShell Gallery](https://img.shields.io/powershellgallery/v/Genbox.FastData.svg?style=flat-square&label=powershell)](https://www.powershellgallery.com/packages/Genbox.FastData/)     |

## Getting started

### Using the executable

1. [Download](https://github.com/Genbox/FastData/releases/latest) the executable
2. Run `FastData rust dogs.txt`

### Using the .NET CLI tool
1. Install the [Genbox.FastData.Cli tool](https://www.nuget.org/packages/Genbox.FastData.Cli/): `dotnet tool install --global Genbox.FastData.Cli`
2. Run `FastData cpp dogs.txt`

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
### Using the C# library
1. Add the [Genbox.FastData.Generator.CSharp](https://www.nuget.org/packages/Genbox.FastData.Generator.CSharp/) NuGet package to your project.
2. Use the `FastDataGenerator.Generate()` method:

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

Generic data structures like arrays, hash tables, etc. are not optimized for your data. If you only need read-only access to a dataset, FastData can provide up to 14x better performance and less memory overhead.

Here is a classic example on just using an array:
```csharp
string[] dogs = ["Labrador", "German Shepherd", "Golden Retriever"];

if (dogs.Contains("Beagle"))
    Console.WriteLine("It contains Beagle");
```

We know our data at compile-time, so why not let a program analyze it and come up with a better way?

FastData produces the following code:
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

- **Early exit:** A single-register bitmap of string lengths allows early termination for string lengths that cannot be in the set.
- **Efficient lookups:** A switch-based data structure which is faster for small datasets.

As a bonus, we also get some metadata about the dataset as constants, which, when used, allows for better code generation by optimizing compiler.

## Features

- **Data analysis:** Optimizes the algorithms on the inherent properties of the dataset.
- **Many data structures:** FastData automatically chooses the best data structure for your data.
- **Fast hashing:** Strings are analyzed and the hash function is specially tailored to the data.
- **Zero dependencies:** The generated code has no dependencies, making it easy to integrate into your project.
- **Minimal memory usage:** The generated data structures are memory-efficient, using only the necessary amount of memory for the dataset.
- **High-perfromance:** The generated data structures are generated without unnecessary branching or virtualization making the compiler produce optimal code.
- **Key/Value support:** FastData can produce key/value lookup data structures

It supports several output programming languages.

* C#: `FastData csharp <input-file>`
* C++: `FastData cpp <input-file>`
* Rust: `FastData rust <input-file>`

Each output language has different settings. Run `FastData <lang> --help` to see the options.

## Benchmarks

A benchmark of .NET's `Array`, `HashSet<T>` and `FrozenSet<T>` versus FastData's auto-generated data structure really illustrates the difference.

| Method    | Categories |      Mean | Factor |
|-----------|------------|----------:|-------:|
| Array     | InSet      | 6.5198 ns |      - |
| HashSet   | InSet      | 6.2191 ns |  1.05x |
| FrozenSet | InSet      | 1.6010 ns |  4.07x |
| FastData  | InSet      | 0.9378 ns |  6.95x |
|           |            |           |        |
| Array     | NotInSet   | 7.4015 ns |      - |
| HashSet   | NotInSet   | 4.6013 ns |  1.61x |
| FrozenSet | NotInSet   | 1.5816 ns |  4.68x |
| FastData  | NotInSet   | 0.5284 ns | 14.01x |

Bigger factor means faster query times.

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

#### Does it support key/value lookup?
Yes, you can specify key/value arrays as input data and FastData will generate a efficient key lookup function that returns a value.

#### Are there any best pratcies for using FastData?
* Put the most often queried items first in the input data. It can speed up query speed for some data structures.

#### Can I use it for dynamic data?
No, FastData is designed for static data only. It generates code at compile time, so the data must be known beforehand.