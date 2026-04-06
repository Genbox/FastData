using Genbox.FastData.Internal.Extensions;
using Genbox.FastData.Internal.Structures;

namespace Genbox.FastData.Tests;

public class TypeExtensionsTests
{
    [Fact]
    public void GetFriendlyName_StripsStructureSuffix()
    {
        string name = typeof(HashTableCompactStructure<,>).GetFriendlyName();
        Assert.Equal("HashTableCompact", name);
    }
}