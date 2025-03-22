using System.Text;

namespace Genbox.FastData.HashFunctions;

//TODO: This is a very bad implementation right now. It is going away soon.

public static class PJWHash
{
    public static string GetString(string input, int[] positions)
    {
        StringBuilder sb = new StringBuilder();

        foreach (int i in positions)
        {
            char c;

            if (i == -1)
                c = input[input.Length - 1];
            else if (i <= input.Length - 1)
                c = input[i];
            else
                continue;

            sb.Append(c);
        }

        return sb.ToString();
    }

    public static uint Hash(string input, int[] positions)
    {
        string s = GetString(input, positions);

        //This hash function is PJW hash

        uint h = 0;
        for (int i = 0; i < s.Length; i++)
        {
            h = (h << 4) + s[i++];

            uint high = h & 0xf0000000;

            if (high != 0)
                h = h ^ (high >> 24) ^ high;
        }
        return h;
    }
}