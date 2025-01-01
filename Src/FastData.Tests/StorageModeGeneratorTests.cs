using Genbox.FastData.Enums;
using Genbox.FastData.Internal;
using static Genbox.FastData.Tests.Code.TestHelper;

namespace Genbox.FastData.Tests;

public class StorageModeGeneratorTests
{
    private static readonly string[] _default = ["item1", "item2", "item3", "item4", "item5", "item6", "item7", "item8", "item9", "item10"];
    private static readonly string[] _uniqueLength = ["a", "aa", "aaa", "aaaa", "aaaaa", "aaaaaa", "aaaaaaa", "aaaaaaaa", "aaaaaaaaa", "aaaaaaaaaa"];
    private static readonly string[] _single = ["item0"];

    [Theory, MemberData(nameof(GetStorageModes))]
    public void RunStorageModes(StorageMode mode, string[] data)
    {
        string source = $"""
                         using Genbox.FastData;
                         using Genbox.FastData.Enums;
                         [assembly: FastData<string>("ImmutableSet", [ {string.Join(',', data.Select(x => $"\"{x}\""))} ], StorageMode = StorageMode.{mode})]
                         """;

        string actual = GetGeneratedOutput<FastDataGenerator>(source);
        File.WriteAllText(@"..\..\..\Generated\StorageModes\" + mode + ".output", actual);
    }

    public static TheoryData<StorageMode, string[]> GetStorageModes() => new TheoryData<StorageMode, string[]>
    {
        { StorageMode.Auto, _default },
        { StorageMode.Array, _default },
        { StorageMode.BinarySearch, _default },
        { StorageMode.EytzingerSearch, _default },
        { StorageMode.Switch, _default },
        { StorageMode.SwitchHashCode, _default },
        { StorageMode.MinimalPerfectHash, _default },
        { StorageMode.HashSet, _default },
        { StorageMode.UniqueKeyLength, _uniqueLength },
        { StorageMode.UniqueKeyLengthSwitch, _uniqueLength },
        { StorageMode.KeyLength, _default },
        { StorageMode.SingleValue, _single },
        { StorageMode.Conditional, _default }
    };
}