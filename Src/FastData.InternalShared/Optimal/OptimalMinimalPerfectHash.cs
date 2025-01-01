using System.Runtime.InteropServices;
using Genbox.FastData.Helpers;

namespace Genbox.FastData.InternalShared.Optimal;

public static class OptimalMinimalPerfectHash
{
    private static readonly Entry[] _entries =
    [
        new Entry("item10", 1446204200u),
        new Entry("item7", 2059837471u),
        new Entry("item5", 1058051832u),
        new Entry("item1", 3715884073u),
        new Entry("item3", 2839760084u),
        new Entry("item2", 3254557275u),
        new Entry("item4", 1109959706u),
        new Entry("item6", 3736370357u),
        new Entry("item9", 3332245378u),
        new Entry("item8", 1738592619u)
    ];

    public static bool Contains(string value)
    {
        if (value.Length is < 5 or > 6)
            return false;

        uint hash = unchecked((uint)HashHelper.Hash(value, 1121));
        uint index = MathHelper.FastMod(hash, 10, 1844674407370955162);
        ref Entry entry = ref _entries[index];

        return hash == entry.HashCode && value == entry.Value;
    }

    [StructLayout(LayoutKind.Auto)]
    private struct Entry(string value, uint hashCode)
    {
        public string Value = value;
        public uint HashCode = hashCode;
    }
}