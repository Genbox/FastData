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
        Add(new EarlyExitVector(new UnitAtLessThanEarlyExit('m'), "alpha", "omega", "offset_0"));
        Add(new EarlyExitVector(new UnitAtGreaterThanEarlyExit('m'), "zulu", "mango", "offset_0"));
        Add(new EarlyExitVector(new UnitAtLessThanEarlyExit('m', -1), "gamma", "jam", "offset_neg1"));
        Add(new EarlyExitVector(new UnitAtGreaterThanEarlyExit('m', -1), "cargo", "ram", "offset_neg1"));
        Add(new EarlyExitVector(new UnitAtNotEqualEarlyExit('x', false), "Xray", "xray", "offset_0_ignoreCase_false"));
        Add(new EarlyExitVector(new UnitAtNotEqualEarlyExit('x', true), "Zulu", "Xray", "offset_0_ignoreCase_true"));
        Add(new EarlyExitVector(new UnitAtNotEqualEarlyExit('x', false, -1), "boX", "box", "offset_neg1_ignoreCase_false"));
        Add(new EarlyExitVector(new UnitAtNotEqualEarlyExit('x', true, -1), "cargo", "boX", "offset_neg1_ignoreCase_true"));
        Add(new EarlyExitVector(new UnitAtBitmapEarlyExit(0ul, (1ul << ('a' - 64)) | (1ul << ('z' - 64)), false), "bravo", "alpha", "offset_0_ignoreCase_false"));
        Add(new EarlyExitVector(new UnitAtBitmapEarlyExit(0ul, (1ul << ('a' - 64)) | (1ul << ('z' - 64)), true), "bravo", "Zulu", "offset_0_ignoreCase_true"));
        Add(new EarlyExitVector(new UnitAtBitmapEarlyExit(0ul, (1ul << ('x' - 64)) | (1ul << ('y' - 64)), false, -1), "cargo", "matrix", "offset_neg1_ignoreCase_false"));
        Add(new EarlyExitVector(new UnitAtBitmapEarlyExit(0ul, (1ul << ('x' - 64)) | (1ul << ('y' - 64)), true, -1), "delta", "proxY", "offset_neg1_ignoreCase_true"));
        Add(new EarlyExitVector(new EqualsAtEarlyExit("pre", 0, false), "alpha", "prefix", "prefix_ignoreCase_false"));
        Add(new EarlyExitVector(new EqualsAtEarlyExit("pre", 0, true), "alpha", "Prelude", "prefix_ignoreCase_true"));
        Add(new EarlyExitVector(new EqualsAtEarlyExit("suf", -3, false), "ending", "endsuf", "suffix_ignoreCase_false"));
        Add(new EarlyExitVector(new EqualsAtEarlyExit("suf", -3, true), "ending", "EndSUF", "suffix_ignoreCase_true"));
        Add(new EarlyExitVector(new ValueLessThanEarlyExit<int>(10), 5, 10));
        Add(new EarlyExitVector(new ValueGreaterThanEarlyExit<int>(20), 42, 20));
        Add(new EarlyExitVector(new ValueNotEqualEarlyExit<int>(10), 11, 10));
        Add(new EarlyExitVector(new ValueBitMaskEarlyExit(0x00FF00ul), 0x000200u, 0x000001u));
    }
}