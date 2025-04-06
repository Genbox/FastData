namespace Genbox.FastData.Internal.Structures;

/// <summary>
/// An array based on Ullman set. It provides O(1) clearing.
/// </summary>
internal sealed class BoolArray
{
    private uint _iterationNumber = 1;
    private readonly uint[] _storageArray;

    /// <summary>
    /// An array based on Ullman set. It provides O(1) clearing.
    /// </summary>
    /// <param name="size">The initial size of the array</param>
    public BoolArray(int size)
    {
        _storageArray = new uint[size];

#if DebugPrint
        Console.WriteLine($"\nbool array size = {size}, total bytes = {size * sizeof(uint)}");
#endif
    }

    public bool SetBit(int index)
    {
        if (_storageArray[index] == _iterationNumber)
            return true;

        _storageArray[index] = _iterationNumber;
        return false;
    }

    public void Clear()
    {
        /* If we wrap around it's time to zero things out again!  However, this only
           occurs once about every 2^32 iterations, so it will not happen more
           frequently than once per second. */
        if (++_iterationNumber == 0)
        {
            _iterationNumber = 1;
            Array.Clear(_storageArray, 0, _storageArray.Length);
        }
    }
}