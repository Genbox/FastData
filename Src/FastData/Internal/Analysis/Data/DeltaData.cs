using System.Runtime.InteropServices;

namespace Genbox.FastData.Internal.Analysis.Data;

[StructLayout(LayoutKind.Auto)]
internal readonly record struct DeltaData(int[]? Left, int[]? Right)
{
    internal int LeftZeroCount => Left == null ? 0 : CountZero(Left);
    internal int RightZeroCount => Right == null ? 0 : CountZero(Right);

    //TODO: See the todo in KeyAnalyzer about supporting prefix/suffix only
    // internal IEnumerable<ArraySegment> GetSegments()
    // {
    //     if (Map == null)
    //         throw new InvalidOperationException("Cannot get map data");
    //
    //     int index = 0;
    //
    //     while (index < Map.Length)
    //     {
    //         while (index < Map.Length && Map[index] != 0)
    //             index++;
    //
    //         if (index >= Map.Length)
    //             break;
    //
    //         int start = index;
    //
    //         while (index < Map.Length && Map[index] == 0)
    //             index++;
    //
    //         yield return new ArraySegment((uint)start, index, Alignment.Left);
    //     }
    // }

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