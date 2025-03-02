using System.Collections;

namespace Genbox.FastData.Internal.Analysis.Genetic;

/// <summary>
/// This exists as a perf optimization. We want to avoid moving too many buffers around, so this list uses tombstoning instead.
/// </summary>
internal class PopulationList(int initialSize) : IEnumerable<int>
{
    private int[] _population = new int[initialSize];
    private bool[] _tombstones = new bool[initialSize];
    private int _length;

    public void Add(int i)
    {
        if (_length + 1 > _population.Length)
        {
            Array.Resize(ref _population, _length + 10);
            Array.Resize(ref _tombstones, _length + 10);
        }

        _population[_length++] = i;
    }

    public void Exclude(int i) => _tombstones[i] = true;
    public void Include(int i) => _tombstones[i] = false;

    public void Clear()
    {
        //We don't have to clear the arrays.
        _length = 0;
    }

    public IEnumerator<int> GetEnumerator() => Enumerable.Range(0, _length).Where(x => !_tombstones[x]).Select(x => _population[x]).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}