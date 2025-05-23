using Genbox.FastData.Abstracts;

namespace Genbox.FastData.Specs.Hash;

public sealed record HeuristicStringHash(List<int> Positions) : IStringHash
{
    public HashFunc<string> GetHashFunction() => obj => Hash(obj, Positions);
    public EqualFunc<string> GetEqualFunction() => (a, b) => Equal(a, b, Positions);

    private static uint Hash(string input, List<int> positions)
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

    private static bool Equal(string a, string b, List<int> positions)
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
}