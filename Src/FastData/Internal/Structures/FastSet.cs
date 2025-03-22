using System.Runtime.InteropServices;

namespace Genbox.FastData.Internal.Structures;

public ref struct FastSet(uint capacity, Func<string, uint> hashFunc, Func<string, string, bool> equalFunc)
{
    private readonly int[] _buckets = new int[capacity];
    private readonly Entry[] _entries = new Entry[capacity];
    private int _count;

    //TODO: optimize
    public readonly int MinVariance => _buckets.Where(x => x != 0).Min();
    public readonly int MaxVariance => _buckets.Max();

    public bool Add(string value)
    {
        uint hashCode = hashFunc(value);
        ref int bucket = ref _buckets[hashCode % capacity];
        int i = bucket - 1;

        while (i >= 0)
        {
            Entry entry = _entries[i];

            if (entry.Hash == hashCode && equalFunc(entry.Value, value))
                return false;

            i = entry.Next;
        }

        ref Entry newEntry = ref _entries[_count];
        newEntry.Hash = hashCode;
        newEntry.Next = bucket - 1;
        newEntry.Value = value;
        bucket = _count + 1;

        _count++;
        return true;
    }

    [StructLayout(LayoutKind.Auto)]
    private record struct Entry(uint Hash, int Next, string Value);
}