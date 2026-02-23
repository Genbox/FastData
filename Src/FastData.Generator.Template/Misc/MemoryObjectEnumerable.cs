using System.Collections;

namespace Genbox.FastData.Generator.Template.Misc;

public sealed class MemoryObjectEnumerable<T>(ReadOnlyMemory<T> memory) : IEnumerable<object>
{
    public IEnumerator<object> GetEnumerator() => new Enumerator(memory);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class Enumerator(ReadOnlyMemory<T> memory) : IEnumerator<object>
    {
        private int _index = -1;

        public object Current => memory.Span[_index];

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            _index++;
            return _index < memory.Length;
        }

        public void Dispose() {}

        public void Reset() => throw new NotSupportedException("not supported");
    }
}