using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Pgm;

namespace Genbox.FastData.Internal.Structures;

/// <remarks>
/// The reference PGM-index is a runtime library. Here it is used as an ahead-of-time code generator
/// where all segment data is baked into static arrays. Segment data is stored as parallel arrays (SoA)
/// in the generated code rather than an array of structs (AoS), for better cache locality during
/// segment key lookup.
/// </remarks>
public sealed class PgmStructure<TKey, TValue> : IStructure<TKey, TValue, PgmContext<TKey, TValue>> where TKey : notnull
{
    private readonly int _epsilon;
    private readonly int _epsilonRecursive;
    private readonly bool _keysAreSorted;

    internal PgmStructure(bool keysAreSorted, int epsilon = 64, int epsilonRecursive = 4)
    {
        if (typeof(TKey) != typeof(int) &&
            typeof(TKey) != typeof(uint) &&
            typeof(TKey) != typeof(long) &&
            typeof(TKey) != typeof(ulong) &&
            typeof(TKey) != typeof(short) &&
            typeof(TKey) != typeof(ushort) &&
            typeof(TKey) != typeof(byte) &&
            typeof(TKey) != typeof(sbyte) &&
            typeof(TKey) != typeof(char) &&
            typeof(TKey) != typeof(float) &&
            typeof(TKey) != typeof(double))
            throw new InvalidOperationException("Unsupported key type");

        _keysAreSorted = keysAreSorted;
        _epsilon = epsilon;
        _epsilonRecursive = epsilonRecursive;
    }

    public PgmContext<TKey, TValue> Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values)
    {
        if (!_keysAreSorted)
        {
            TKey[] keysCopy = new TKey[keys.Length];
            keys.CopyTo(keysCopy);

            TValue[] valuesCopy = new TValue[values.Length];
            values.CopyTo(valuesCopy);

            if (values.IsEmpty)
                Array.Sort(keysCopy);
            else
                Array.Sort(keysCopy, valuesCopy);

            keys = keysCopy;
            values = valuesCopy;
        }

        if (HasSentinelKey(keys))
            throw new ArgumentException($"The value {PgmTypeTraits<TKey>.MaxValue} is reserved as a sentinel.", nameof(keys));

        PgmBuilder<TKey>.Result result = PgmBuilder<TKey>.BuildIndex(keys, _epsilon, _epsilonRecursive);

        return new PgmContext<TKey, TValue>(keys, values, result.Segments, result.LevelsOffsets, _epsilon, _epsilonRecursive, result.SegmentCount);
    }

    public IEnumerable<IEarlyExit> GetMandatoryExits() => [];

    private static bool HasSentinelKey(ReadOnlyMemory<TKey> keys) => Comparer<TKey>.Default.Compare(keys.Span[keys.Length - 1], PgmTypeTraits<TKey>.MaxValue) == 0;
}