using Genbox.FastData.Abstracts;
using Genbox.FastData.Specs;

namespace Genbox.FastData.ArrayHash;

public sealed record HeuristicArrayHash(List<int> Positions) : IArrayHash
{
    public HashFunc GetHashFunction()
    {
        return null!;

        // return (obj, l) => Hash(obj, Positions);
    }

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

    public override string ToString() => $"Positions = {string.Join(",", Positions)}";
}