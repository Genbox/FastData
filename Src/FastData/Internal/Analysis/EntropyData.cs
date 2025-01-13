using System.Runtime.InteropServices;

namespace Genbox.FastData.Internal.Analysis;

[StructLayout(LayoutKind.Auto)]
internal readonly record struct EntropyData(int[] Left, int[] Right)
{
    /// <summary>Gets the entropy map with the highest number of leading zeroes, as well as the number of leading zeroes</summary>
    internal (bool Left, int[] Data, int Length) GetJustify()
    {
        int leftCount = CountZero(Left);
        int rightCount = CountZero(Right);

        return leftCount >= rightCount ? (true, Left, leftCount) : (false, Right, rightCount);
    }

    private static int CountZero(int[] data)
    {
        int count;
        for (count = 0; count < data.Length; count++)
        {
            if (data[count] != 0)
                break;
        }
        return count;
    }
}