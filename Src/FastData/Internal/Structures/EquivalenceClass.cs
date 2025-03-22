namespace Genbox.FastData.Internal.Structures;

internal class EquivalenceClass
{
    // The keywords in this equivalence class.
    public readonly List<Keyword> Keywords = new List<Keyword>();

    // The number of keywords in this equivalence class.
    public uint Cardinality;

    // The undetermined selected characters for the keywords in this equivalence class, as a canonically reordered multiset.
    public uint[] UndeterminedChars;
    public int UndeterminedCharsLength;

    public EquivalenceClass? Next;
};