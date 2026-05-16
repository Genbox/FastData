using Genbox.FastData.Generators.EarlyExits.Exits;

namespace Genbox.FastData.TestHarness.Runner.Code.Theory;

public sealed class EarlyExitVectors : TheoryData<EarlyExitVector>
{
    public EarlyExitVectors()
    {
        Add(new EarlyExitVector(new LengthNotEqualEarlyExit(4), "route", "road"));
        Add(new EarlyExitVector(new LengthLessThanEarlyExit(2), "a", "ab"));
        Add(new EarlyExitVector(new LengthGreaterThanEarlyExit(5), "letters", "hello"));
        Add(new EarlyExitVector(new LengthBitmapEarlyExit((1ul << (3 - 1)) | (1ul << (6 - 1))), "index", "cat"));
        Add(new EarlyExitVector(new CharOffsetLessThanEarlyExit('m', 0), "alpha", "omega", "offset_0"));
        Add(new EarlyExitVector(new CharOffsetGreaterThanEarlyExit('m', 0), "zulu", "mango", "offset_0"));
        Add(new EarlyExitVector(new CharOffsetLessThanEarlyExit('m', -1), "gamma", "jam", "offset_neg1"));
        Add(new EarlyExitVector(new CharOffsetGreaterThanEarlyExit('m', -1), "cargo", "ram", "offset_neg1"));
        Add(new EarlyExitVector(new CharOffsetNotEqualEarlyExit('x', false, 0), "Xray", "xray", "offset_0_ignoreCase_false"));
        Add(new EarlyExitVector(new CharOffsetNotEqualEarlyExit('x', true, 0), "Zulu", "Xray", "offset_0_ignoreCase_true"));
        Add(new EarlyExitVector(new CharOffsetNotEqualEarlyExit('x', false, -1), "boX", "box", "offset_neg1_ignoreCase_false"));
        Add(new EarlyExitVector(new CharOffsetNotEqualEarlyExit('x', true, -1), "cargo", "boX", "offset_neg1_ignoreCase_true"));
        Add(new EarlyExitVector(new CharOffsetBitmapEarlyExit(0ul, (1ul << ('a' - 64)) | (1ul << ('z' - 64)), false, 0), "bravo", "alpha", "offset_0_ignoreCase_false"));
        Add(new EarlyExitVector(new CharOffsetBitmapEarlyExit(0ul, (1ul << ('a' - 64)) | (1ul << ('z' - 64)), true, 0), "bravo", "Zulu", "offset_0_ignoreCase_true"));
        Add(new EarlyExitVector(new CharOffsetBitmapEarlyExit(0ul, (1ul << ('x' - 64)) | (1ul << ('y' - 64)), false, -1), "cargo", "matrix", "offset_neg1_ignoreCase_false"));
        Add(new EarlyExitVector(new CharOffsetBitmapEarlyExit(0ul, (1ul << ('x' - 64)) | (1ul << ('y' - 64)), true, -1), "delta", "proxY", "offset_neg1_ignoreCase_true"));
        Add(new EarlyExitVector(new StringAtEarlyExit("pre", 0, false), "alpha", "prefix", "prefix_ignoreCase_false"));
        Add(new EarlyExitVector(new StringAtEarlyExit("pre", 0, true), "alpha", "Prelude", "prefix_ignoreCase_true"));
        Add(new EarlyExitVector(new StringAtEarlyExit("suf", -3, false), "ending", "endsuf", "suffix_ignoreCase_false"));
        Add(new EarlyExitVector(new StringAtEarlyExit("suf", -3, true), "ending", "EndSUF", "suffix_ignoreCase_true"));
        Add(new EarlyExitVector(new ValueLessThanEarlyExit<int>(10), 5, 10));
        Add(new EarlyExitVector(new ValueGreaterThanEarlyExit<int>(20), 42, 20));
        Add(new EarlyExitVector(new ValueNotEqualEarlyExit<int>(10), 11, 10));
        Add(new EarlyExitVector(new ValueBitMaskEarlyExit(0x00FF00ul), 0x000200u, 0x000001u));
    }
}