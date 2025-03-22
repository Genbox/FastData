namespace Genbox.FastData.Internal.Structures;

internal class Keyword
{
    public Keyword(string allChars)
    {
        AllChars = allChars;
    }

    public string AllChars { get; set; }
    public string SelChars { get; set; }
    public int HashValue { get; set; }
    public int FinalIndex { get; set; } = -1;

    internal void InitSelCharsMultiset(int[] positions, int[] alpha_inc)
    {
        char[] selchars = init_selchars_low(positions, alpha_inc);

        // Sort the selchars elements alphabetically.
        Array.Sort(selchars);

        SelChars = new string(selchars);
    }

    internal void InitSelCharsTuple(int[] positions) => init_selchars_low(positions);

    private char[] init_selchars_low(int[] positions, int[]? alpha_inc = null)
    {
        // Iterate through the list of positions, initializing selchars (via ptr).
        char[] key_set = new char[positions.Length];

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

            key_set[ptr++] = (char)c;
        }

        SelChars = new string(key_set);
        return key_set;
    }
}