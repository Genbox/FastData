using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Internal.Analysis.Hashing;

internal class HighEntropyGenerator : IHashGenerator
{
    public bool IsAppropriate(StringProperties stringProps) => stringProps.LengthData.Max > 8;

    // The idea behind this generator is to read the entropy maps made during string analysis and derive
    // a hash function that uses the characters that change most often (highest entropy).
    // The goal is to use as few characters as possible

    public IEnumerable<StrRange> Generate(StringProperties stringProps)
    {
        (_, int[] Data, _) = stringProps.EntropyData.GetJustify();

        // We skip zero entropy characters
        int nonZero = Data.Count(x => x != 0);
        int[] newData = new int[nonZero];

        int j = 0;
        for (int i = 0; i < Data.Length; i++)
        {
            if (Data[i] == 0)
                continue;

            newData[j++] = Data[i];
        }

        // Create an array with indexes.
        int[] entropy = new int[newData.Length];
        for (int i = 0; i < entropy.Length; i++)
            entropy[i] = i;

        // Sort the indexes according to en absolute entropy values
        Array.Sort(newData, entropy, Comparer<int>.Create((a, b) => Math.Abs(a).CompareTo(Math.Abs(b))));

        // Now we have a single dimension array, sorted by entropy, where the value is the index (in Data) where the entropy value resides
        // Walk from the top (most entropy) and add each index that is lower than the smallest string

        int[] candidates = new int[stringProps.LengthData.Min];
        j = 0;
        for (int i = 0; i < entropy.Length; i++)
        {
            // The char must be at an offset smaller than the smallest string.
            if (entropy[i] > stringProps.LengthData.Min)
                continue;

            candidates[j++] = entropy[i];
        }

        // Return a single char candidate for each of the highest entropy characters
        foreach (int candidate in candidates)
            yield return new StrRange((uint)entropy[candidate], 1);

        //TODO: pick a candidate and expand it in one or both directions: x, xb, ax, axb
    }
}