using System.Runtime.InteropServices;

namespace Genbox.FastData.Internal.Analysis;

[StructLayout(LayoutKind.Auto)]
internal readonly record struct EntropyData(int[] Left, int[] Right)
{
    internal (bool Left, int Length) GetJustify()
    {
        int leftCount = CountZero(Left);
        int rightCount = CountZero(Right);

        return leftCount >= rightCount ? (true, leftCount) : (false, rightCount);
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