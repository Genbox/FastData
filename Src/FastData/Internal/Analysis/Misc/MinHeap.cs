namespace Genbox.FastData.Internal.Analysis.Misc;

/// <summary>
/// Min-heap of fixed capacity that stores values with associated items.
/// Maintains the smallest element at the root; when full, adding a larger value replaces the root.
/// </summary>
public class MinHeap<T>(int capacity)
{
    private readonly int _capacity = capacity;
    private readonly double[] _keys = new double[capacity];
    private readonly T[] _values = new T[capacity];
    private int _count;

    /// <summary>
    /// Adds a new value-item pair. If capacity not reached, inserts and restores heap.
    /// If full and value &gt; root, replaces root and restores heap.
    /// </summary>
    public void Add(double value, T item)
    {
        if (_count < _capacity)
        {
            _keys[_count] = value;
            _values[_count] = item;
            MoveUp(_count);
            _count++;
        }
        else if (value > _keys[0])
        {
            _keys[0] = value;
            _values[0] = item;
            MoveDown(0);
        }
    }

    private void MoveUp(int i)
    {
        while (i > 0)
        {
            int parent = (i - 1) / 2;
            if (_keys[i] >= _keys[parent]) break;
            Swap(i, parent);
            i = parent;
        }
    }

    private void MoveDown(int i)
    {
        while (true)
        {
            int left = (2 * i) + 1;
            int right = (2 * i) + 2;
            int smallest = i;

            if (left < _count && _keys[left] < _keys[smallest])
                smallest = left;
            if (right < _count && _keys[right] < _keys[smallest])
                smallest = right;
            if (smallest == i) break;

            Swap(i, smallest);
            i = smallest;
        }
    }

    private void Swap(int i, int j)
    {
        (_keys[i], _keys[j]) = (_keys[j], _keys[i]);
        (_values[i], _values[j]) = (_values[j], _values[i]);
    }

    public IEnumerable<double> Keys
    {
        get
        {
            for (int i = 0; i < _count; i++)
                yield return _keys[i];
        }
    }

    public IEnumerable<T> Values
    {
        get
        {
            for (int i = 0; i < _count; i++)
                yield return _values[i];
        }
    }

    public void Clear()
    {
        Array.Clear(_keys, 0, _count);
        Array.Clear(_values, 0, _count);
        _count = 0;
    }
}