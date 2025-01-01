using System.Runtime.InteropServices;
using Genbox.FastData.Helpers;

namespace Genbox.FastData.InternalShared.Optimal;

public static class OptimalHashSet
{
    private static readonly int[] _buckets = [3, 10, 4, 2, 9, 8, 6, 0, 7, 1];

    private static readonly Entry[] _entries =
    [
        new Entry(1547457659, -1, "item1"),
        new Entry(3756450423, -1, "item2"),
        new Entry(731309710, -1, "item3"),
        new Entry(111896862, -1, "item4"),
        new Entry(120110704, -1, "item5"),
        new Entry(236029066, -1, "item6"),
        new Entry(3119466618, -1, "item7"),
        new Entry(1860819585, -1, "item8"),
        new Entry(968102074, 4, "item9"),
        new Entry(1975224761, -1, "item10")
    ];

    public static bool Contains(string value)
    {
        if (value.Length is < 5 or > 6)
            return false;

        uint hashCode = unchecked((uint)HashHelper.Hash(value));
        uint index = MathHelper.FastMod(hashCode, 10, 1844674407370955162);
        int i = _buckets[index] - 1;

        while (i >= 0)
        {
            ref Entry entry = ref _entries[i];

            if (entry.HashCode == hashCode && entry.Value == value)
                return true;

            i = entry.Next;
        }

        return false;
    }

    [StructLayout(LayoutKind.Auto)]
    private struct Entry(uint hashCode, short next, string value)
    {
        public uint HashCode = hashCode;
        public short Next = next;
        public string Value = value;
    }
}