using Genbox.FastData.Abstracts;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal;
using Genbox.FastData.InternalShared;

namespace Genbox.FastData.Tests;

public class FunctionalityTests
{
    [Fact]
    public void OutputIsUniq()
    {
        Assert.Throws<InvalidOperationException>(() => CodeGenerator.DynamicCreateSet<FastDataGenerator>(["item", "item"], StorageMode.Array, false));
    }

    [Theory, MemberData(nameof(GetImpl))]
    public void Contains(IFastSet set, StorageMode _, int size)
    {
        Assert.True(set.Contains("a"));
        Assert.False(set.Contains("dontexist"));

        //Also test if length is correct
        Assert.Equal(size, set.Length);
    }

    public static TheoryData<IFastSet, StorageMode, int> GetImpl()
    {
        TheoryData<IFastSet, StorageMode, int> data = new TheoryData<IFastSet, StorageMode, int>();

        StorageMode[] modes = Enum.GetValues<StorageMode>().Where(x => x != StorageMode.SingleValue).ToArray();
        int[] sizes = [1, 16]; //We don't support not setting any values, but we do support sets of 1.

        foreach (int size in sizes)
        {
            List<string> items = new List<string>(size);

            for (int i = 1; i < size + 1; i++)
                items.Add(new string('a', i)); //We generate unique lengths to satisfy the constraints for the UniqueLength index

            foreach (StorageMode mode in modes)
            {
                IFastSet set = CodeGenerator.DynamicCreateSet<FastDataGenerator>(items, mode, false);
                data.Add(set, mode, size);
            }
        }

        return data;
    }
}