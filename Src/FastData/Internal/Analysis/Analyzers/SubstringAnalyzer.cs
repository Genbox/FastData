using Genbox.FastData.Config.Analysis;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators.StringHash;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Enums;
using Genbox.FastData.Internal.Helpers;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Internal.Analysis.Analyzers;

internal sealed class SubstringAnalyzer(StringKeyProperties props, SubstringAnalyzerConfig config, Simulator sim, GeneratorEncoding encoding, bool ignoreCase = false) : IStringHashAnalyzer
{
    public bool IsAppropriate() => !ignoreCase || props.CharacterData.AllAscii;

    public IEnumerable<Candidate> GetCandidates(ReadOnlySpan<string> data)
    {
        int maxLength = Math.Min(config.MaxSliceByteLength, props.LengthData.MinByteLength);
        if (maxLength <= 0)
            return [];

        List<Candidate> candidates = new List<Candidate>(config.MaxReturned * 2);

        // Keep segments aligned to the target encoding's lookup unit. For C# this avoids hashing half of a UTF-16 code unit.
        for (int length = sim.UnitSize; length <= maxLength; length += sim.UnitSize)
        {
            for (int offset = 0; offset + length <= props.LengthData.MinByteLength; offset += sim.UnitSize)
            {
                TryAdd(data, candidates, new ArraySegment((uint)offset, length, Alignment.Left));
                TryAdd(data, candidates, new ArraySegment((uint)offset, length, Alignment.Right));
            }
        }

        candidates.Sort(static (a, b) => b.Fitness.CompareTo(a.Fitness));

        if (candidates.Count <= config.MaxReturned)
            return candidates;

        candidates.RemoveRange(config.MaxReturned, candidates.Count - config.MaxReturned);
        return candidates;
    }

    private void TryAdd(ReadOnlySpan<string> data, List<Candidate> candidates, ArraySegment segment)
    {
        int unique = CountUnique(data, segment);
        double uniqueFraction = unique / (double)data.Length;

        if (uniqueFraction < config.MinUniqueFraction)
            return;

        SubstringStringHash stringHash = new SubstringStringHash(segment, ignoreCase, sim.UnitSize);
        candidates.Add(sim.Run(data, stringHash, () => uniqueFraction));
    }

    private int CountUnique(ReadOnlySpan<string> data, ArraySegment segment)
    {
        HashSet<byte[]> seen = new HashSet<byte[]>(ByteArrayComparer.Instance);

        foreach (string key in data)
        {
            byte[] bytes = StringHelper.GetBytesFunc(encoding)(key);
            int start = segment.Alignment == Alignment.Right ? bytes.Length - (int)segment.Offset - segment.Length : (int)segment.Offset;
            byte[] slice = new byte[segment.Length];

            if (ignoreCase)
            {
                for (int i = 0; i < slice.Length; i++)
                    slice[i] = (byte)(bytes[start + i] | 0x20);
            }
            else
            {
                Array.Copy(bytes, start, slice, 0, slice.Length);
            }

            seen.Add(slice);
        }

        return seen.Count;
    }

    private sealed class ByteArrayComparer : IEqualityComparer<byte[]>
    {
        internal static ByteArrayComparer Instance { get; } = new ByteArrayComparer();

        public bool Equals(byte[]? x, byte[]? y) => ReferenceEquals(x, y) || (x != null && y != null && x.AsSpan().SequenceEqual(y));

        public int GetHashCode(byte[] obj)
        {
            unchecked
            {
                int hash = 17;
                foreach (byte b in obj)
                    hash = (hash * 31) + b;

                return hash;
            }
        }
    }
}