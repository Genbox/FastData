using Genbox.FastData.Config.Analysis;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Generators.Contexts.Misc;
using Genbox.FastData.Generators.EarlyExits.Exits;
using Genbox.FastData.Generators.StringHash;
using Genbox.FastData.Generators.StringHash.Framework;
using Genbox.FastData.Internal;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Enums;
using Genbox.FastData.Internal.Helpers;
using Genbox.FastData.Internal.Misc;
using Genbox.FastData.Internal.Structures;
using Microsoft.Extensions.Logging.Abstractions;

namespace Genbox.FastData.Tests;

public class SubstringStringHashTests
{
    [Fact]
    public void GetBestHash_SharedPrefix_UsesRightSubstring()
    {
        string[] data = Enumerable.Range(0, 16).Select(static x => $"shared-prefix-{x:x4}").ToArray();
        Candidate candidate = GetCandidate(data);
        SubstringStringHash hash = Assert.IsType<SubstringStringHash>(candidate.StringHash);

        Assert.Equal(Alignment.Right, hash.Segment.Alignment);
        AssertLookups(data, hash.GetExpression().Compile());
    }

    [Fact]
    public void GetBestHash_SharedSuffix_UsesLeftSubstring()
    {
        string[] data = Enumerable.Range(0, 16).Select(static x => $"{x:x4}-shared-suffix").ToArray();
        Candidate candidate = GetCandidate(data);
        SubstringStringHash hash = Assert.IsType<SubstringStringHash>(candidate.StringHash);

        Assert.Equal(Alignment.Left, hash.Segment.Alignment);
        AssertLookups(data, hash.GetExpression().Compile());
    }

    [Fact]
    public void GetMandatoryExits_ReturnsSegmentLengthGuard()
    {
        SubstringStringHash hash = new SubstringStringHash(new ArraySegment(2, 4, Alignment.Right));
        IEarlyExit exit = Assert.Single(hash.GetMandatoryExits());

        LengthLessThanEarlyExit length = Assert.IsType<LengthLessThanEarlyExit>(exit);
        Assert.Equal(6, length.Value);
    }

    private static Candidate GetCandidate(string[] data)
    {
        StringKeyProperties props = KeyAnalyzer.GetStringProperties(data, false, GeneratorEncoding.AsciiBytes);
        StringAnalyzerConfig config = new StringAnalyzerConfig
        {
            BenchmarkIterations = 100,
            SubstringAnalyzerConfig = new SubstringAnalyzerConfig { MinUniqueFraction = 1 },
            BruteForceAnalyzerConfig = null,
            GeneticAnalyzerConfig = null,
            GPerfAnalyzerConfig = null
        };

        return HashBenchmark.GetBestHash(data, props, config, NullLoggerFactory.Instance, GeneratorEncoding.AsciiBytes, false);
    }

    private static void AssertLookups(string[] data, StringHashFunc func)
    {
        HashTableContext<string, byte> context = CreateHashTable(data, func);

        foreach (string key in data)
            Assert.True(Contains(context, func, key), $"Did not find {key}");

        Assert.False(Contains(context, func, "not-in-data"));
    }

    private static HashTableContext<string, byte> CreateHashTable(string[] data, StringHashFunc func)
    {
        HashData hashData = HashData.Create(data, 1, x =>
        {
            byte[] bytes = StringHelper.GetBytesFunc(GeneratorEncoding.AsciiBytes)(x);
            return func(bytes, bytes.Length);
        });

        HashTableStructure<string, byte> structure = new HashTableStructure<string, byte>(hashData);
        return structure.Create(data, ReadOnlyMemory<byte>.Empty);
    }

    private static bool Contains(HashTableContext<string, byte> context, StringHashFunc func, string key)
    {
        byte[] bytes = StringHelper.GetBytesFunc(GeneratorEncoding.AsciiBytes)(key);
        ulong hash = func(bytes, bytes.Length);
        int entryIndex = context.Buckets[hash % (ulong)context.Buckets.Length] - 1;

        while (entryIndex >= 0)
        {
            HashTableEntry<string> entry = context.Entries[entryIndex];
            if (entry.Hash == hash && StringComparer.Ordinal.Equals(entry.Key, key))
                return true;

            entryIndex = entry.Next;
        }

        return false;
    }
}