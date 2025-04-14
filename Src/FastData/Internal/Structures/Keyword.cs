using System.Diagnostics.CodeAnalysis;

namespace Genbox.FastData.Internal.Structures;

internal class Keyword(string allChars)
{
    public string AllChars { get; } = allChars;
    public string SelChars { get; private set; }
    public int HashValue { get; set; }

    internal void InitSelCharsMultiset(int[] positions, int[] alpha_inc)
    {
        char[] chars = InitSelCharsLow(positions, alpha_inc);
        Array.Sort(chars);
        SelChars = new string(chars);
    }

    internal void InitSelCharsTuple(int[] positions)
    {
        char[] chars = InitSelCharsLow(positions);
        SelChars = new string(chars);
    }

    [SuppressMessage("Performance", "MA0159:Use \'Order\' instead of \'OrderBy\'")]
    private char[] InitSelCharsLow(int[] positions, int[]? alpha_inc = null)
    {
        Span<char> keySet = stackalloc char[positions.Length];

        int ptr = 0;
        foreach (int i in positions.OrderBy(x => x))
        {
            if (i >= AllChars.Length)
                continue;

            int c;

            if (i == -1)
                c = AllChars[AllChars.Length - 1];
            else if (i < AllChars.Length)
            {
                c = AllChars[i];

                if (alpha_inc != null)
                    c += alpha_inc[i];
            }
            else
                continue;

            keySet[ptr++] = (char)c;
        }

        return keySet.Slice(0, ptr).ToArray();
    }
}