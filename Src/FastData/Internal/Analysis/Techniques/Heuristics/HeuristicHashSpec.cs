using System.Runtime.InteropServices;
using Genbox.FastData.Abstracts;
using Genbox.FastData.HashFunctions;

namespace Genbox.FastData.Internal.Analysis.Techniques.Heuristics;

[StructLayout(LayoutKind.Auto)]
internal readonly record struct HeuristicHashSpec(int[] Positions) : IHashSpec
{
    public Func<string, uint> GetFunction()
    {
        int[] localPos = Positions;
        return x => PJWHash.Hash(x, localPos);
    }

    public Func<string, string, bool> GetEqualFunction()
    {
        int[] localPos = Positions;
        return (a, b) => PJWHash.GetString(a, localPos) == PJWHash.GetString(b, localPos);
    }

    public string GetSource()
        => $$"""
                 private static int[] _positions = { {{string.Join(", ", Positions)}} };

                 public static uint Hash(string str)
                 {
                     return Genbox.FastData.HashFunctions.PJWHash.Hash(str, _positions);
                 }
             """;
}