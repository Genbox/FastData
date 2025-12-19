using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides a context for ranged-based data structures.</summary>
[SuppressMessage("Major Code Smell", "S2326:Unused type parameters should be removed")]
public sealed class RangeContext<TKey, TValue>(TValue[]? values) : IContext<TValue>
{
    public TValue[]? Values { get; } = values;
}