using System.Runtime.InteropServices;

namespace Genbox.FastData.Internal.Analysis.Data;

[StructLayout(LayoutKind.Auto)]
internal readonly record struct DeltaData(int[]? Left, int[]? Right)
{
    internal int LeftZeroCount => Left == null ? 0 : CountZero(Left);
    internal int RightZeroCount => Right == null ? 0 : CountZero(Right);

    internal static int CountZero(int[] data)
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