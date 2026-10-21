namespace Genbox.FastData.Internal.Analysis.Misc;

/// <summary>Fixed-capacity min-heap of scored items. The lowest score is at the root; when full, adding a higher score replaces the root.</summary>
internal sealed class MinHeap<T>(int capacity)
{
    private readonly int _capacity = capacity;
    private readonly (double, T)[] _items = new (double, T)[capacity];
    private double _best = double.MinValue;
    private int _count;

    public bool HasMaxFitness => Math.Abs(_best - 1) < double.Epsilon;

    public IEnumerable<(double, T)> Items
    {
        get
        {
            for (int i = 0; i < _count; i++)
                yield return _items[i];
        }
    }

    /// <summary>Adds a scored item. Below capacity it is inserted; when full, it replaces the root if <paramref name="key" /> exceeds the root's score.</summary>
    /// <returns><see langword="true" /> if <paramref name="key" /> exceeds every score added so far; otherwise, <see langword="false" />.</returns>
    public bool Add(double key, T value)
    {
        if (_count < _capacity)
        {
            _items[_count] = (key, value);
            MoveUp(_count);
            _count++;

            if (key > _best)
            {
                _best = key;
                return true;
            }
        }
        else if (key > _items[0].Item1)
        {
            _items[0] = (key, value);
            MoveDown(0);

            if (key > _best)
            {
                _best = key;
                return true;
            }
        }

        return false;
    }

    private void MoveUp(int i)
    {
        while (i > 0)
        {
            int parent = (i - 1) / 2;

            if (_items[i].Item1 >= _items[parent].Item1)
                break;

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

            if (left < _count && _items[left].Item1 < _items[smallest].Item1)
                smallest = left;
            if (right < _count && _items[right].Item1 < _items[smallest].Item1)
                smallest = right;
            if (smallest == i)
                break;

            Swap(i, smallest);
            i = smallest;
        }
    }

    private void Swap(int i, int j) => (_items[i], _items[j]) = (_items[j], _items[i]);

    public void Clear()
    {
        _count = 0;
        _best = double.MinValue;
    }
}