using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal;
using Genbox.FastData.Internal.Misc;
using Genbox.FastData.Internal.Structures;

namespace Genbox.FastData.Tests;

public sealed class HybleStructureTests
{
    [Theory]
    [InlineData(32)]
    [InlineData(256)]
    [InlineData(1_000)]
    public void CreatesLargerStructures(int count)
    {
        int[] keys = new int[count];

        for (int i = 0; i < keys.Length; i++)
            keys[i] = i;

        HashData hashData = HashData.Create(keys, 1f, static key => SplitMix64.Next((uint)key));
        HybleStructure<int, byte> structure = new HybleStructure<int, byte>(hashData);

        HybleContext<int, byte>? context = structure.Create(keys, ReadOnlyMemory<byte>.Empty);

        Assert.NotNull(context);
        HashSet<(int Key, ulong Hash)> entries = new HashSet<(int Key, ulong Hash)>();

        foreach (KeyValuePair<int, ulong> pair in context.Data)
            entries.Add((pair.Key, pair.Value));

        for (int i = 0; i < keys.Length; i++)
        {
            ulong expectedHash;
            unchecked { expectedHash = SplitMix64.Next((uint)i) * context.Seed; }

            Assert.Contains((i, expectedHash), entries);
        }
    }

    [Fact]
    public void RetainsWinningSeededHashesAndDistinctSentinel()
    {
        string[] keys = ["alpha", "bravo", "charlie", "delta", "echo", "foxtrot"];
        ulong[] baseHashes = [11, 22, 33, 44, 55, 66];
        HashData hashData = HashData.Create(keys, 1f, key => baseHashes[Array.IndexOf(keys, key)]);
        HybleStructure<string, int> structure = new HybleStructure<string, int>(hashData);

        HybleContext<string, int>? context = structure.Create(keys, ReadOnlyMemory<int>.Empty);

        Assert.NotNull(context);
        Assert.True(context.Data.Length > keys.Length);
        HashSet<ulong> expectedHashes = new HashSet<ulong>();

        for (int i = 0; i < keys.Length; i++)
        {
            ulong expectedHash;
            unchecked { expectedHash = baseHashes[i] * context.Seed; }

            expectedHashes.Add(expectedHash);
            Assert.Contains(context.Data, pair => pair.Key == keys[i] && pair.Value == expectedHash);
        }

        Assert.Contains(context.Data, pair => !expectedHashes.Contains(pair.Value));
    }
}