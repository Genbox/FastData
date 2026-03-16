using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits;

namespace Genbox.FastData.TestHarness.Runner.Code.Theory;

public sealed record EarlyExitVector(string Id, IEarlyExit EarlyExit);

public sealed class EarlyExitExpressions : TheoryData<EarlyExitVector>
{
    public EarlyExitExpressions()
    {
        Add(new EarlyExitVector(nameof(LengthEqualEarlyExit), new LengthEqualEarlyExit(4, 8)));
        Add(new EarlyExitVector(nameof(LengthRangeEarlyExit), new LengthRangeEarlyExit(2, 5, 4, 10)));
        Add(new EarlyExitVector(nameof(LengthBitmapEarlyExit), new LengthBitmapEarlyExit([10ul, 5ul])));
        Add(new EarlyExitVector(nameof(CharRangeEarlyExit), new CharRangeEarlyExit(CharPosition.First, 'a', 'z')));
        Add(new EarlyExitVector(nameof(CharEqualsEarlyExit), new CharEqualsEarlyExit(CharPosition.Last, 'x')));
        Add(new EarlyExitVector(nameof(CharBitmapEarlyExit), new CharBitmapEarlyExit(CharPosition.First, 1ul, 2ul)));
        Add(new EarlyExitVector(nameof(StringBitMaskEarlyExit), new StringBitMaskEarlyExit(65535ul, 4)));
        Add(new EarlyExitVector(nameof(StringPrefixSuffixEarlyExit), new StringPrefixSuffixEarlyExit("pre", "suf")));
        Add(new EarlyExitVector(nameof(ValueRangeEarlyExit<>), new ValueRangeEarlyExit<int>(10, 20)));
        Add(new EarlyExitVector(nameof(ValueBitMaskEarlyExit<>), new ValueBitMaskEarlyExit<ulong>(65280ul)));
    }
}