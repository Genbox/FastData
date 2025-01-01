using Genbox.FastData.InternalShared.Optimal;

namespace Genbox.FastData.Tests;

public class OptimalTests
{
    [Fact]
    public void ContainsTest()
    {
        string[] entries =
        [
            "item1",
            "item2",
            "item3",
            "item4",
            "item5",
            "item6",
            "item7",
            "item8",
            "item9",
            "item10"
        ];

        foreach (string e in entries)
        {
            Assert.True(OptimalArray.Contains(e));
            Assert.True(OptimalBinarySearch.Contains(e));
            Assert.True(OptimalConditional.Contains(e));
            Assert.True(OptimalEytzingerSearch.Contains(e));
            Assert.True(OptimalHashSet.Contains(e));
            Assert.True(OptimalKeyLength.Contains(e));
            Assert.True(OptimalMinimalPerfectHash.Contains(e));
            Assert.True(OptimalSwitch.Contains(e));
            Assert.True(OptimalSwitchHashCode.Contains(e));
        }

        //This one only supports a single value
        Assert.True(OptimalSingleValue.Contains("item1"));

        string[] uniqLen =
        [
            "a",
            "aa",
            "aaa",
            "aaaa",
            "aaaaa",
            "aaaaaa",
            "aaaaaaa",
            "aaaaaaaa",
            "aaaaaaaaa",
            "aaaaaaaaaa"
        ];

        //These only support unique lengths
        foreach (string e in uniqLen)
        {
            Assert.True(OptimalUniqueKeyLength.Contains(e));
            Assert.True(OptimalUniqueKeyLengthSwitch.Contains(e));
        }

        Assert.False(OptimalArray.Contains("notthere"));
        Assert.False(OptimalBinarySearch.Contains("notthere"));
        Assert.False(OptimalConditional.Contains("notthere"));
        Assert.False(OptimalEytzingerSearch.Contains("notthere"));
        Assert.False(OptimalHashSet.Contains("notthere"));
        Assert.False(OptimalKeyLength.Contains("notthere"));
        Assert.False(OptimalMinimalPerfectHash.Contains("notthere"));
        Assert.False(OptimalSingleValue.Contains("notthere"));
        Assert.False(OptimalSwitch.Contains("notthere"));
        Assert.False(OptimalSwitchHashCode.Contains("notthere"));
        Assert.False(OptimalUniqueKeyLength.Contains("notthere"));
        Assert.False(OptimalUniqueKeyLengthSwitch.Contains("notthere"));
    }
}