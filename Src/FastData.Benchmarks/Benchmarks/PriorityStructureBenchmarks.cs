using Genbox.FastData.Internal.Analysis.Misc;

namespace Genbox.FastData.Benchmarks.Benchmarks;

[InvocationCount(1_000_000)]
public class PriorityStructureBenchmarks
{
    private readonly MinHeap<bool> _heap = new MinHeap<bool>(10);
    private readonly RingBuffer _buffer = new RingBuffer(10);
    private readonly FixedSet _fixedSet = new FixedSet(10);
    private readonly SortedSet<double> _sorted = new SortedSet<double>();

    [IterationCleanup]
    public void Cleanup()
    {
        _heap.Clear();
        _buffer.Clear();
        _fixedSet.Clear();
        _sorted.Clear();
    }

    [Benchmark]
    public void MinHeapTest()
    {
        for (double i = 0; i < 100; i++)
            _heap.Add(i, true);
    }

    [Benchmark]
    public void RingBufferTest()
    {
        for (double i = 0; i < 100; i++)
            _buffer.Add(i);
    }

    [Benchmark]
    public void FixedSetTest()
    {
        for (double i = 0; i < 100; i++)
            _fixedSet.Add(i);
    }

    [Benchmark]
    public void SortedSetTest()
    {
        for (double i = 0; i < 100; i++)
            _sorted.Add(i);
    }

    private sealed class FixedSet(int capacity)
    {
        private readonly double[] _heap = new double[capacity];
        private int _count;

        public void Add(double value)
        {
            if (_count < capacity)
            {
                _heap[_count] = value;
                _count++;
            }
            else
            {
                for (int i = 0; i < _heap.Length; i++)
                {
                    double val = _heap[i];
                    if (value > val)
                        _heap[i] = value;
                }
            }
        }

        public void Clear()
        {
            Array.Clear(_heap, 0, _count);
            _count = 0;
        }
    }

    private sealed class RingBuffer(int capacity)
    {
        private readonly double[] _buffer = new double[capacity];
        private int _count;
        private int _next;
        private int _minIndex = -1;

        public void Add(double value)
        {
            if (_count < _buffer.Length)
            {
                _buffer[_next] = value;

                if (_minIndex < 0 || value < _buffer[_minIndex])
                    _minIndex = _next;

                _next = (_next + 1) % _buffer.Length;
                _count++;
            }
            else if (value > _buffer[_minIndex])
            {
                _buffer[_minIndex] = value;
                int mi = 0;
                double mv = _buffer[0];
                for (int i = 1; i < _buffer.Length; i++)
                {
                    if (_buffer[i] < mv)
                    {
                        mv = _buffer[i];
                        mi = i;
                    }
                }
                _minIndex = mi;
            }
        }

        public void Clear()
        {
            Array.Clear(_buffer, 0, _count);
            _minIndex = 0;
            _next = 0;
            _count = 0;
        }
    }
}