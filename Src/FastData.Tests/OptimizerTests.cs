using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Optimization;
using Genbox.FastData.Internal.Optimization.StringSpecs;

namespace Genbox.FastData.Tests;

public class OptimizerTests
{
    // [Theory]
    // [InlineData(128, new[] { "abcd", "dcba" })]
    // [InlineData(256, new[] { "abcdabcd", "dcbadcba" })]
    // [InlineData(512, new[] { "abcdabcdabcdabcd", "dcbadcbadcbadcba" })]
    // public void VectorComparerTest(uint vectorLength, string[] data)
    // {
    //     IStringSpec comparerSpec = GetSpec(data);
    //
    //     VectorStringSpec spec = Assert.IsType<VectorStringSpec>(comparerSpec);
    //     Assert.Equal(vectorLength, spec.VectorLength);
    // }

    [Theory]
    [InlineData(true, new[] { "kfhk2t2j", "gj0h202", "guh02pohj", "ajsd0j", "fj+pqfa", "faj08hyg2hy" })]
    [InlineData(true, new[] { "2134", "481815", "19841", "91", "2475752", "10184" })]
    [InlineData(true, new[] { "😀😀", "😁", "🤣", "😉", "😊", "😇" })] //First entries is two symbols to avoid it becoming a vector comparer
    public void FullComparerTest(bool _, string[] data) => Assert.IsType<FullStringSpec>(GetSpec(data));

    private static  IStringSpec GetSpec(string[] data) => Optimizer.GetOptimalSpec(Analyzer.Analyze(data));
}