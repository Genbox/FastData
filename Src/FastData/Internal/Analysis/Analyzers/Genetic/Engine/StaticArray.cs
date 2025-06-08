using System.Collections;

namespace Genbox.FastData.Internal.Analysis.Analyzers.Genetic.Engine;

internal sealed class StaticArray<T>(int capacity) : IEnumerable<T> where T : struct
{
    private readonly T[] _array = new T[capacity];

    internal int Count { get; private set; }

    internal ref T this[int index] => ref _array[index];

    public IEnumerator<T> GetEnumerator() => _array.AsEnumerable().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal void Add(T item) => _array[Count++] = item;
    internal void Add(ref T item) => _array[Count++] = item;

    public void Clear()
    {
        Array.Clear(_array, 0, Count);
        Count = 0;
    }
}