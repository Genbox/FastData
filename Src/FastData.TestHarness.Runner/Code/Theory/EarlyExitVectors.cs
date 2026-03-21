using Genbox.FastData.Generators.EarlyExits;

namespace Genbox.FastData.TestHarness.Runner.Code.Theory;

public sealed class EarlyExitVectors : TheoryData<EarlyExitVector>
{
    public EarlyExitVectors()
    {
        Add(new EarlyExitVector(new LengthNotEqualEarlyExit(4), "route", "road"));
        Add(new EarlyExitVector(new LengthLessThanEarlyExit(2), "a", "ab"));
        Add(new EarlyExitVector(new LengthGreaterThanEarlyExit(5), "letters", "hello"));
        Add(new EarlyExitVector(new LengthBitmapEarlyExit((1ul << (3 - 1)) | (1ul << (6 - 1))), "index", "cat"));
        Add(new EarlyExitVector(new CharFirstLessThanEarlyExit('m'), "alpha", "omega"));
        Add(new EarlyExitVector(new CharFirstGreaterThanEarlyExit('m'), "zulu", "mango"));
        Add(new EarlyExitVector(new CharLastLessThanEarlyExit('m'), "gamma", "jam"));
        Add(new EarlyExitVector(new CharLastGreaterThanEarlyExit('m'), "cargo", "ram"));
        Add(new EarlyExitVector(new CharFirstNotEqualEarlyExit('x', false), "Xray", "xray", "ignoreCase_false"));
        Add(new EarlyExitVector(new CharFirstNotEqualEarlyExit('x', true), "Zulu", "Xray", "ignoreCase_true"));
        Add(new EarlyExitVector(new CharLastNotEqualEarlyExit('x', false), "boX", "box", "ignoreCase_false"));
        Add(new EarlyExitVector(new CharLastNotEqualEarlyExit('x', true), "cargo", "boX", "ignoreCase_true"));
        Add(new EarlyExitVector(new CharFirstBitmapEarlyExit(0ul, (1ul << ('a' - 64)) | (1ul << ('z' - 64)), false), "bravo", "alpha", "ignoreCase_false"));
        Add(new EarlyExitVector(new CharFirstBitmapEarlyExit(0ul, (1ul << ('a' - 64)) | (1ul << ('z' - 64)), true), "bravo", "Zulu", "ignoreCase_true"));
        Add(new EarlyExitVector(new CharLastBitmapEarlyExit(0ul, (1ul << ('x' - 64)) | (1ul << ('y' - 64)), false), "cargo", "matrix", "ignoreCase_false"));
        Add(new EarlyExitVector(new CharLastBitmapEarlyExit(0ul, (1ul << ('x' - 64)) | (1ul << ('y' - 64)), true), "delta", "proxY", "ignoreCase_true"));
        Add(new EarlyExitVector(new StringPrefixEarlyExit("pre", false), "alpha", "prefix", "ignoreCase_false"));
        Add(new EarlyExitVector(new StringPrefixEarlyExit("pre", true), "alpha", "Prelude", "ignoreCase_true"));
        Add(new EarlyExitVector(new StringSuffixEarlyExit("suf", false), "ending", "endsuf", "ignoreCase_false"));
        Add(new EarlyExitVector(new StringSuffixEarlyExit("suf", true), "ending", "EndSUF", "ignoreCase_true"));
        Add(new EarlyExitVector(new ValueLessThanEarlyExit<int>(10), 5, 10));
        Add(new EarlyExitVector(new ValueGreaterThanEarlyExit<int>(20), 42, 20));
        Add(new EarlyExitVector(new ValueNotEqualEarlyExit<int>(10), 11, 10));
        Add(new EarlyExitVector(new ValueBitMaskEarlyExit(0x00FF00ul), 0x000200u, 0x000001u));
    }
}