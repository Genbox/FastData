using System.Diagnostics.CodeAnalysis;

namespace Genbox.FastData.Internal.Helpers;

internal sealed class SwitchArray(uint capacity)
{
    private readonly uint[] _data = new uint[capacity];
    private uint _counter;

    public bool this[uint index]
    {
        get => _data[index] == _counter;
        [SuppressMessage("Blocker Code Smell", "S3237:\"value\" contextual keyword should be used")]
        [SuppressMessage("ReSharper", "ValueParameterNotUsed")]
        set => _data[index] = _counter;
    }

    public void Clear()
    {
        _counter++;

        if (_counter == uint.MaxValue)
        {
            Array.Clear(_data, 0, _data.Length);
            _counter = 0;
        }
    }
}