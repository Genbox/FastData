using System.Diagnostics;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Generators.Extensions;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Data;

namespace Genbox.FastData.Internal.Structures;

public sealed class RangeStructure<TKey, TValue> : IStructure<TKey, TValue, RangeContext<TKey>>
{
    private readonly (TKey Start, TKey End)[] _ranges;

    internal RangeStructure(DataRanges<TKey> ranges)
    {
        // Floating-point ranges are not exact membership sets. For example, keys [1.0, 3.0]
        // would produce a min/max range that also accepts 2.0.
        Debug.Assert(Type.GetTypeCode(typeof(TKey)).IsIntegral(), "RangeStructure requires integral keys.");
        _ranges = ranges.Ranges.ToArray();
    }

    public RangeContext<TKey> Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values) => new RangeContext<TKey>(_ranges);

    public IEnumerable<IEarlyExit> GetMandatoryExits() => [];
}