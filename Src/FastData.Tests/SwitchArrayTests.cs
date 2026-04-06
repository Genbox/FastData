using Genbox.FastData.Internal.Helpers;

namespace Genbox.FastData.Tests;

public class SwitchArrayTests
{
    [Fact]
    public void IndexerTracksSetUntilClear()
    {
        SwitchArray array = new SwitchArray(4);

        Assert.False(array[1]);

        array[1] = true;
        Assert.True(array[1]);

        array.Clear();

        Assert.False(array[1]);

        array[2] = true;
        Assert.False(array[1]);
        Assert.True(array[2]);
    }
}