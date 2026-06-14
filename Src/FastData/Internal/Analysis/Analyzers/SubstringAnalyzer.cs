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
    private readonly Func<string, byte[]> _getBytes = StringHelper.GetBytesFunc(encoding);

    public bool IsAppropriate() => encoding != GeneratorEncoding.Unknown && config.MaxReturned > 0 && config.MaxSliceByteLength > 0 && config.MinUniqueFraction > 0 && (!ignoreCase || props.CharacterData.AllAscii);

    public IEnumerable<Candidate> GetCandidates(ReadOnlySpan<string> data)
    {
        int maxLength = Math.Min(config.MaxSliceByteLength, props.LengthData.MinByteLength);
        if (maxLength <= 0)
            return [];

        List<Candidate> candidates = new List<Candidate>(config.MaxReturned * 2);

        for (int length = 1; length <= maxLength; length <<= 1)
        {
            TryAdd(data, candidates, new ArraySegment(0, length, Alignment.Left));
            TryAdd(data, candidates, new ArraySegment(0, length, Alignment.Right));
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

        SubstringStringHash stringHash = new SubstringStringHash(segment, ignoreCase);
        candidates.Add(sim.Run(data, stringHash, () => uniqueFraction));
    }

    private int CountUnique(ReadOnlySpan<string> data, ArraySegment segment)
    {
        HashSet<ulong> seen = new HashSet<ulong>();

        foreach (string key in data)
        {
            byte[] bytes = _getBytes(key);
            ulong value = 0;
            int start = segment.Alignment == Alignment.Right ? bytes.Length - segment.Length : 0;

            for (int i = 0; i < segment.Length; i++)
            {
                byte b = bytes[start + i];
                if (ignoreCase)
                    b = (byte)(b | 0x20);

                value |= (ulong)b << (i * 8);
            }

            seen.Add(value);
        }

        return seen.Count;
    }
}