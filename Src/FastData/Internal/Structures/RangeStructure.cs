using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Data;

namespace Genbox.FastData.Internal.Structures;

public sealed class RangeStructure<TKey, TValue> : IStructure<TKey, TValue, RangeContext<TKey>>
{
    private readonly (TKey Start, TKey End)[] _ranges;

    internal RangeStructure(DataRanges<TKey> ranges)
    {
        _ranges = ranges.Ranges.ToArray();
    }

    public RangeContext<TKey> Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values) => new RangeContext<TKey>(_ranges);

    public IEnumerable<IEarlyExit> GetMandatoryExits() => [];
}