using System.Numerics;
using System.Runtime.InteropServices;
using Genbox.FastData.Abstracts;

namespace Genbox.FastData.Internal.Analysis.Techniques.Heuristics;

[StructLayout(LayoutKind.Auto)]
internal readonly record struct HeuristicHashSpec(HashSet<int> positions) : IHashSpec
{
    public Func<string, uint> GetFunction() => Hash;

    private const uint PRIME32_1 = 0x9E3779B1U;
    private const uint PRIME32_2 = 0x85EBCA77U;
    private const uint PRIME32_3 = 0xC2B2AE3DU;
    private const uint PRIME32_5 = 0x165667B1U;

    private uint Hash(string input)
    {
        uint hash = PRIME32_5 + (uint)input.Length;
        foreach (int i in positions.OrderBy(x => x))
        {
            char c;

            if (i == -1)
                c = input[input.Length - 1];
            else if (i <= input.Length - 1)
                c = input[i];
            else
                continue;

            hash ^= c * PRIME32_5;
            hash = BitOperations.RotateLeft(hash, 11) * PRIME32_1;
        }

        hash ^= hash >> 15;
        hash *= PRIME32_2;
        hash ^= hash >> 13;
        hash *= PRIME32_3;
        hash ^= hash >> 16;
        return hash;
    }

    public string GetSource()
        => """
               [MethodImpl(MethodImplOptions.AggressiveInlining)]
               public static uint Hash(string input)
               {
                   uint hash = PRIME32_5 + (uint)input.Length;
                   foreach (int i in positions)
                   {
                       char c;

                       if (i == -1)
                           c = input[input.Length - 1];
                       else if (i <= input.Length - 1)
                           c = input[i];
                       else
                           continue;

                       hash ^= c * PRIME32_5;
                       hash = BitOperations.RotateLeft(hash, 11) * PRIME32_1;
                   }

                   hash ^= hash >> 15;
                   hash *= PRIME32_2;
                   hash ^= hash >> 13;
                   hash *= PRIME32_3;
                   hash ^= hash >> 16;
                   return hash;
               }
           """;
}