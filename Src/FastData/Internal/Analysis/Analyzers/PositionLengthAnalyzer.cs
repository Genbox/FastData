using Genbox.FastData.Config.Analysis;
using Genbox.FastData.Generators.StringHash;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Internal.Analysis.Analyzers;

internal sealed class PositionLengthAnalyzer(StringKeyProperties props, PositionLengthAnalyzerConfig config, Simulator sim, bool ignoreCase = false) : IStringHashAnalyzer
{
    public bool IsAppropriate() => props.LengthData.MinByteLength >= sim.UnitSize;

    public IEnumerable<Candidate> GetCandidates(ReadOnlySpan<string> data)
    {
        bool lengthUseful = config.IncludeLength && props.LengthData.MinByteLength != props.LengthData.MaxByteLength;
        bool firstCharUseful = !props.CharacterData.AllAscii || props.CharacterData.FirstCharMap.BitCount > 1;
        bool lastCharUseful = config.IncludeLastChar && (!props.CharacterData.AllAscii || props.CharacterData.LastCharMap.BitCount > 1);
        bool lastCharDistinct = props.LengthData.MinByteLength != sim.UnitSize || props.LengthData.MaxByteLength != sim.UnitSize;

        List<Candidate> candidates = new List<Candidate>(7);

        if (lengthUseful)
            candidates.Add(sim.Run(data, Create([], true)));

        if (firstCharUseful)
            candidates.Add(sim.Run(data, Create([0], false)));

        if (firstCharUseful && lengthUseful)
            candidates.Add(sim.Run(data, Create([0], true)));

        if (lastCharUseful && lastCharDistinct)
            candidates.Add(sim.Run(data, Create([-1], false)));

        if (lastCharUseful && lastCharDistinct && lengthUseful)
            candidates.Add(sim.Run(data, Create([-1], true)));

        if (firstCharUseful && lastCharUseful && lastCharDistinct)
            candidates.Add(sim.Run(data, Create([0, -1], false)));

        if (firstCharUseful && lastCharUseful && lastCharDistinct && lengthUseful)
            candidates.Add(sim.Run(data, Create([0, -1], true)));

        return candidates;
    }

    private PositionLengthStringHash Create(int[] positions, bool includeLength) => new PositionLengthStringHash(positions, includeLength, 1, sim.UnitSize, ignoreCase);
}