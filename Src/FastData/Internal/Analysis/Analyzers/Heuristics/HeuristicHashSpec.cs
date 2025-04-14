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
        return x => Hash(x, localPos);
    }

    public EqualFunc GetEqualFunction()
    {
        int[] localPos = Positions;
        return (a, b) => Equal(a, b, localPos);
    }

    private static bool Equal(string a, string b, int[] positions)
    {
        foreach (int pos in positions)
        {
            if (pos == -1) //This if-case should come first, or else it will overlap with the next
            {
                if (a[a.Length - 1] != b[b.Length - 1])
                    return false;
            }
            else if (pos <= a.Length - 1 && pos <= b.Length - 1)
            {
                if (a[pos] != b[pos])
                    return false;
            }
        }

        return true;
    }

    private static uint Hash(string input, int[] positions)
    {
        //This hash function is PJW hash

        uint h = 0;
        foreach (int pos in positions)
        {
            char c;

            if (pos == -1)
                c = input[input.Length - 1];
            else if (pos <= input.Length - 1)
                c = input[pos];
            else
                continue;

            h = (h << 4) + c;

            uint high = h & 0xf0000000;

            if (high != 0)
                h = h ^ (high >> 24) ^ high;
        }
        return h;
    }
}