using System.Runtime.InteropServices;
using Genbox.FastData.Abstracts;
using Genbox.FastData.HashFunctions;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Heuristics;

[StructLayout(LayoutKind.Auto)]
internal readonly record struct HeuristicHashSpec(int[] Positions) : IHashSpec
{
    public HashFunc GetHashFunction()
    {
        int[] localPos = Positions;
        return x => PJWHash.Hash(x, localPos);
    }

    public EqualFunc GetEqualFunction()
    {
        int[] localPos = Positions;
        return (a, b) => PJWHash.GetString(a, localPos) == PJWHash.GetString(b, localPos);
    }
}