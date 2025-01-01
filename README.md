# FastData

[![NuGet](https://img.shields.io/nuget/v/Genbox.ReplaceMe.svg?style=flat-square&label=nuget)](https://www.nuget.org/packages/Genbox.ReplaceMe/)
[![License](https://img.shields.io/github/license/Genbox/ReplaceMe)](https://github.com/Genbox/ReplaceMe/blob/master/LICENSE.txt)

### Description
This project is a .NET source generator to create read-only lookup-datastructures of immutable data.

### Use case
Let's say you create a game with a resource system. Resources are named "character01", "health_texture01", etc. You need to quickly check if a resource is present, so you add all your resources to an array and call `Contains()` on it like so:

```csharp
string[] resources = [ "character01", "health_texture01", ... ];

if (resources.Contains("player01"))
{
    // Do something
}
```
This works just fine while you are developing the game, but once it starts having a lot more resources, the `Contains()` check will start to slow down the game considerably due to its O(n) complexity.
It is easy to fix by changing to a `HashSet<string>` and benchmarking to ensure that it performs better, but you'll have to constantly track all the sets of resources you have and change the implementation when adding/removing large amounts of resources.

With this source generator, you don't have to think about it. It will dynamically change the underlying data structure to match your data. It ensures optimal query time and low memory usage.
There is only one requirement, and that is that all the values in your set is known up front and it never changes.

### Features

* Data analysis to utilize the properties of your data to gain even more benefits. See the Data Analysis section for details.
* Support for several indexing structures:
   * Array with linear search
   * Array with binary search
   * Array with Eytzinger layout (and binary search)
   * Array indexed with string length
   * Array indexed with hash codes
   * Array indexed with a minimal perfect hash function
* Supports `StringComparison` so you can choose how items are compared. Defaults to Ordinal.
* Zero runtime overhead. All the data structures are generated at compile time.

### Example

```csharp
static async Task Main()
{
    //CODE HERE
}
```

Output:

```
//OUTPUT HERE
```

### How does it work?
The idea behind the project is to generate a data-dependent optimized data structure for read-only access/lookup. When data is known beforehand, the algorithm can select from a set of different data structures, indexing and comparison methods that are tailor built for the data.
Let's say you have a set of names. You put them in an array and users can lookup if their name is in the array.

```csharp
var names = { "john", "chris", "mike", "ruben", "jack" };
var input = "...";

Console.WriteLine("Name is in the list: " + name.Contains(input));
```
Seems simple, but what happens if the set grows to 1000 names? or 1 mio. names? Is the latency of the lookup going to keep being consistent?
Let's benchmark 1 mio. names using a `string[]` and a `HashSet<string>` in C#:

| Method  | Mean           |
|-------- |---------------:|
| HashSet |       132.7 ns |
| Array   | 1,782,259.4 ns |

We can see that HashSet is 13,501 times faster.
This is a challenge in a lot of applications. For example in compilers (language keywords), games (asset keys), and routed systems (route keys).

## Supported data structures
A data structure is the layout of the data itself. Usually you might just want to store the data in an array, but that might not be the best layout depending on the size of the array.
Instead it might be prudent to store it in a hash table or perhaps even in a tree structure. There are several data structures supported:
* Array - O(n)
* Sorted Array - O(n log n)
* Eytzinger Array - O(n log n)
* Dictionary - O(1)
* Conditional (switch-case) - O(n)