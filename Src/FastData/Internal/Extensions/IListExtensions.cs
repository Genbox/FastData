using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Extensions;

internal static class IListExtensions
{
    internal static void Shuffle<T>(this IList<T> list, IRandom random)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}