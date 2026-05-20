using System.Text;
using Genbox.FastData.Config.Analysis;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Generators.Contexts.Misc;
using Genbox.FastData.Generators.EarlyExits.Exits;
using Genbox.FastData.Generators.StringHash;
using Genbox.FastData.Generators.StringHash.Framework;
using Genbox.FastData.Internal;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Analyzers;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Helpers;
using Genbox.FastData.Internal.Structures;
using Microsoft.Extensions.Logging.Abstractions;

namespace Genbox.FastData.Tests;

public class GPerfAnalyzerTests
{
    public static IEnumerable<object[]> UpstreamGPerfDataSets()
    {
        // Keyword sets copied from gperf tests.
        yield return ["c.gperf", new[] { "if", "do", "int", "for", "case", "char", "auto", "goto", "else", "long", "void", "enum", "float", "short", "union", "break", "while", "const", "double", "static", "extern", "struct", "return", "sizeof", "switch", "signed", "typedef", "default", "unsigned", "continue", "register", "volatile" }, false];
        yield return ["c++.gperf", new[] { "asm", "auto", "break", "case", "catch", "char", "class", "const", "continue", "default", "delete", "do", "double", "else", "enum", "extern", "float", "for", "friend", "goto", "if", "inline", "int", "long", "new", "operator", "overload", "private", "protected", "public", "register", "return", "short", "signed", "sizeof", "static", "struct", "switch", "template", "this", "typedef", "union", "unsigned", "virtual", "void", "volatile", "while" }, false];
        yield return ["ada.gperf", new[] { "else", "exit", "terminate", "type", "raise", "range", "reverse", "declare", "end", "record", "exception", "not", "then", "return", "separate", "select", "digits", "renames", "subtype", "elsif", "function", "for", "package", "procedure", "private", "while", "when", "new", "entry", "delay", "case", "constant", "at", "abort", "accept", "and", "delta", "access", "abs", "pragma", "array", "use", "out", "do", "others", "of", "or", "all", "limited", "loop", "null", "task", "in", "is", "if", "rem", "mod", "begin", "body", "xor", "goto", "generic", "with" }, false];
        yield return ["adadefs.gperf", new[] { "boolean", "character", "constraint_error", "false", "float", "integer", "natural", "numeric_error", "positive", "program_error", "storage_error", "string", "tasking_error", "true", "address", "aft", "base", "callable", "constrained", "count", "delta", "digits", "emax", "epsilon", "first", "firstbit", "fore", "image", "large", "last", "lastbit", "length", "machine_emax", "machine_emin", "machine_mantissa", "machine_overflows", "machine_radix", "machine_rounds", "mantissa", "pos", "position", "pred", "range", "safe_emax", "safe_large", "safe_small", "size", "small", "storage_size", "succ", "terminated", "val", "value", "width" }, false];
        yield return ["modula3.gperf", new[] { "AND", "ARRAY", "BEGIN", "BITS", "BY", "CASE", "CONST", "DIV", "DO", "ELSE", "ELSIF", "END", "EVAL", "EXCEPT", "EXCEPTION", "EXIT", "EXPORTS", "FINALLY", "FOR", "FROM", "IF", "IMPORT", "INTERFACE", "IN", "INLINE", "LOCK", "METHODS", "MOD", "MODULE", "NOT", "OBJECT", "OF", "OR", "PROCEDURE", "RAISES", "READONLY", "RECORD", "REF", "REPEAT", "RETURN", "SET", "THEN", "TO", "TRY", "TYPE", "TYPECASE", "UNSAFE", "UNTIL", "UNTRACED", "VALUE", "VAR", "WHILE", "WITH", "and", "array", "begin", "bits", "by", "case", "const", "div", "do", "else", "elsif", "end", "eval", "except", "exception", "exit", "exports", "finally", "for", "from", "if", "import", "interface", "in", "inline", "lock", "methods", "mod", "module", "not", "object", "of", "or", "procedure", "raises", "readonly", "record", "ref", "repeat", "return", "set", "then", "to", "try", "type", "typecase", "unsafe", "until", "untraced", "value", "var", "while", "with" }, false];
        yield return ["pascal.gperf", new[] { "with", "array", "and", "function", "case", "var", "const", "until", "then", "set", "record", "program", "procedure", "or", "packed", "not", "nil", "label", "in", "repeat", "of", "goto", "forward", "for", "while", "file", "else", "downto", "do", "div", "to", "type", "end", "mod", "begin", "if" }, false];
        yield return ["permut2.gperf", new[] { "xy", "yx", "xz", "zx" }, false];
        yield return ["permut3.gperf", new[] { "abc", "acb", "bca", "cab" }, false];
        yield return ["permutc2.gperf", new[] { "az", "za", "ay", "ya", "x{", "x[", "{w", "[w" }, true];
        yield return ["smtp.gperf", GetSmtpHeaderFields(), true];
    }

    [Theory]
    [MemberData(nameof(UpstreamGPerfDataSets))]
    public void GetCandidates_UpstreamGPerfDataSets_GeneratesUsableHash(string name, string[] data, bool ignoreCase)
    {
        Candidate candidate = GetCandidate(data, ignoreCase);
        StringHashFunc func = candidate.StringHash.GetExpression().Compile();
        AssertRawHashesUnique(name, data, func, GeneratorEncoding.AsciiBytes);

        HashTableContext<string, byte> context = CreateHashTable(data, func, GeneratorEncoding.AsciiBytes);
        StringComparer comparer = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

        foreach (string key in data)
            Assert.True(Contains(context, func, key, comparer, GeneratorEncoding.AsciiBytes), $"{name} did not find {key}");

        Assert.False(Contains(context, func, "Dave", comparer, GeneratorEncoding.AsciiBytes), $"{name} unexpectedly found Dave");
    }

    [Fact]
    public void GetCandidates_IgnoreCase_UsesUnifiedAssociationValues()
    {
        string[] data = ["while", "return", "switch", "case"];
        Candidate candidate = GetCandidate(data, true);
        StringHashFunc func = candidate.StringHash.GetExpression().Compile();

        byte[] lower = "return"u8.ToArray();
        byte[] upper = "RETURN"u8.ToArray();

        Assert.Equal(func(lower, lower.Length), func(upper, upper.Length));
    }

    [Fact]
    public void GetCandidates_UpstreamSmtpIgnoreCase_UsesUnifiedAssociationValues()
    {
        Candidate candidate = GetCandidate(GetSmtpHeaderFields(), true);
        StringHashFunc func = candidate.StringHash.GetExpression().Compile();

        byte[] original = "Content-MD5"u8.ToArray();
        byte[] swappedCase = "cONTENT-md5"u8.ToArray();

        Assert.Equal(func(original, original.Length), func(swappedCase, swappedCase.Length));
    }

    [Fact]
    public void GetCandidates_UniqueLengths_AllowsLengthOnlyHash()
    {
        string[] data = ["a", "bb", "ccc", "dddd"];
        Candidate candidate = GetCandidate(data, false);
        StringHashFunc func = candidate.StringHash.GetExpression().Compile();

        foreach (string key in data)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(key);
            Assert.Equal((ulong)bytes.Length, func(bytes, bytes.Length));
        }
    }

    [Fact]
    public void GetCandidates_KeyPositions_UsesOneBasedBytePositions()
    {
        string[] data = ["\u00e9a", "\u00e9b"];
        GPerfAnalyzerConfig config = new GPerfAnalyzerConfig { KeyPositions = "3", NoLength = true };
        Candidate candidate = GetCandidate(data, false, GeneratorEncoding.Utf8Bytes, config);
        GPerfStringHash hash = Assert.IsType<GPerfStringHash>(candidate.StringHash);
        StringHashFunc func = hash.GetExpression().Compile();

        Assert.Equal(new[] { 2 }, hash.Positions);
        AssertRawHashesUnique("utf8 explicit key position", data, func, GeneratorEncoding.Utf8Bytes);
    }

    [Fact]
    public void GetCandidates_KeyPositions_UsesLastBytePosition()
    {
        string[] data = ["abx", "aby", "abz"];
        GPerfAnalyzerConfig config = new GPerfAnalyzerConfig { KeyPositions = "$", NoLength = true };
        Candidate candidate = GetCandidate(data, false, GeneratorEncoding.AsciiBytes, config);
        GPerfStringHash hash = Assert.IsType<GPerfStringHash>(candidate.StringHash);
        StringHashFunc func = hash.GetExpression().Compile();

        Assert.Equal(new[] { -1 }, hash.Positions);
        AssertRawHashesUnique("last byte key position", data, func, GeneratorEncoding.AsciiBytes);
    }

    [Fact]
    public void GetCandidates_KeyPositions_UsesRanges()
    {
        string[] data = ["abcd", "abce", "abcf"];
        GPerfAnalyzerConfig config = new GPerfAnalyzerConfig { KeyPositions = "1-4", NoLength = true };
        Candidate candidate = GetCandidate(data, false, GeneratorEncoding.AsciiBytes, config);
        GPerfStringHash hash = Assert.IsType<GPerfStringHash>(candidate.StringHash);

        Assert.Equal(new[] { 3, 2, 1, 0 }, hash.Positions);
    }

    [Fact]
    public void GetCandidates_KeyPositions_UsesAllDatasetPositions()
    {
        string[] data = ["ab", "ac"];
        GPerfAnalyzerConfig config = new GPerfAnalyzerConfig { KeyPositions = "*", NoLength = true };
        Candidate candidate = GetCandidate(data, false, GeneratorEncoding.AsciiBytes, config);
        GPerfStringHash hash = Assert.IsType<GPerfStringHash>(candidate.StringHash);
        StringHashFunc func = hash.GetExpression().Compile();

        Assert.Equal(2, hash.Positions.Length);
        Assert.Equal(1, hash.Positions[0]);
        Assert.Equal(0, hash.Positions[^1]);
        AssertRawHashesUnique("all key positions", data, func, GeneratorEncoding.AsciiBytes);
    }

    [Fact]
    public void GetCandidates_KeyPositionsWildcard_UsesSupportedDatasetPositions()
    {
        string[] data = [new string('a', 300)];
        GPerfAnalyzerConfig config = new GPerfAnalyzerConfig { KeyPositions = "*", NoLength = true };
        Candidate candidate = GetCandidate(data, false, GeneratorEncoding.AsciiBytes, config);
        GPerfStringHash hash = Assert.IsType<GPerfStringHash>(candidate.StringHash);

        Assert.Equal(255, hash.Positions.Length);
        Assert.Equal(254, hash.Positions[0]);
        Assert.Equal(0, hash.Positions[^1]);
    }

    [Fact]
    public void GetCandidates_KeyPositions_RejectsPositionsOutsideDataset()
    {
        string[] data = ["ab", "ac"];
        GPerfAnalyzerConfig config = new GPerfAnalyzerConfig { KeyPositions = "3", NoLength = true };

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => GetCandidate(data, false, GeneratorEncoding.AsciiBytes, config));
        Assert.Contains("outside the maximum encoded key length", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GetCandidates_NoLength_OmitsLengthContribution()
    {
        string[] data = ["a", "bb", "ccc"];
        GPerfAnalyzerConfig config = new GPerfAnalyzerConfig { NoLength = true };
        Candidate candidate = GetCandidate(data, false, GeneratorEncoding.AsciiBytes, config);
        GPerfStringHash hash = Assert.IsType<GPerfStringHash>(candidate.StringHash);

        Assert.False(hash.HashIncludesLength);
    }

    [Fact]
    public void GetCandidates_ConfiguredAssociationSearchOptions_GeneratesUsableHash()
    {
        string[] data = ["xy", "yx", "xz", "zx"];
        GPerfAnalyzerConfig config = new GPerfAnalyzerConfig
        {
            InitialAssociationValue = 1,
            Jump = 2,
            SizeMultiple = 0.5
        };
        Candidate candidate = GetCandidate(data, false, GeneratorEncoding.AsciiBytes, config);
        StringHashFunc func = candidate.StringHash.GetExpression().Compile();

        AssertRawHashesUnique("configured association search", data, func, GeneratorEncoding.AsciiBytes);
    }

    [Fact]
    public void GPerfAnalyzerConfig_CreateOptions_ParsesKeyPositions()
    {
        GPerfAnalyzerOptions options = new GPerfAnalyzerConfig { KeyPositions = "1-3,$" }.CreateOptions();

        Assert.Equal(new[] { 0, 1, 2, -1 }, options.KeyPositions);
    }

    [Fact]
    public void GPerfAnalyzerConfig_CreateOptions_AllowsRepeatedCommas()
    {
        GPerfAnalyzerOptions repeatedCommas = new GPerfAnalyzerConfig { KeyPositions = "1,,2," }.CreateOptions();

        Assert.Equal(new[] { 0, 1 }, repeatedCommas.KeyPositions);
    }

    [Fact]
    public void GPerfAnalyzerConfig_CreateOptions_RejectsMalformedWildcardKeyPositions()
    {
        ArgumentException withPosition = Assert.Throws<ArgumentException>(() => new GPerfAnalyzerConfig { KeyPositions = "*,1" }.CreateOptions());
        ArgumentException afterPosition = Assert.Throws<ArgumentException>(() => new GPerfAnalyzerConfig { KeyPositions = "1,*" }.CreateOptions());
        ArgumentException range = Assert.Throws<ArgumentException>(() => new GPerfAnalyzerConfig { KeyPositions = "*-3" }.CreateOptions());

        Assert.Equal(nameof(GPerfAnalyzerConfig.KeyPositions), withPosition.ParamName);
        Assert.Equal(nameof(GPerfAnalyzerConfig.KeyPositions), afterPosition.ParamName);
        Assert.Equal(nameof(GPerfAnalyzerConfig.KeyPositions), range.ParamName);
    }

    [Fact]
    public void GPerfAnalyzerConfig_CreateOptions_RejectsInvalidKeyPositions()
    {
        ArgumentException ex = Assert.Throws<ArgumentException>(() => new GPerfAnalyzerConfig { KeyPositions = "1,$-4" }.CreateOptions());
        ArgumentException range = Assert.Throws<ArgumentException>(() => new GPerfAnalyzerConfig { KeyPositions = "1-1" }.CreateOptions());

        Assert.Equal(nameof(GPerfAnalyzerConfig.KeyPositions), ex.ParamName);
        Assert.Equal(nameof(GPerfAnalyzerConfig.KeyPositions), range.ParamName);
    }

    [Fact]
    public void GPerfAnalyzerConfig_CreateOptions_RejectsDuplicateKeyPositions()
    {
        ArgumentException first = Assert.Throws<ArgumentException>(() => new GPerfAnalyzerConfig { KeyPositions = "1,1" }.CreateOptions());
        ArgumentException last = Assert.Throws<ArgumentException>(() => new GPerfAnalyzerConfig { KeyPositions = "$,$" }.CreateOptions());

        Assert.Equal(nameof(GPerfAnalyzerConfig.KeyPositions), first.ParamName);
        Assert.Equal(nameof(GPerfAnalyzerConfig.KeyPositions), last.ParamName);
    }

    [Fact]
    public void GPerfAnalyzerConfig_CreateOptions_RejectsOutOfRangeKeyPositions()
    {
        ArgumentOutOfRangeException below = Assert.Throws<ArgumentOutOfRangeException>(() => new GPerfAnalyzerConfig { KeyPositions = "0" }.CreateOptions());
        ArgumentOutOfRangeException above = Assert.Throws<ArgumentOutOfRangeException>(() => new GPerfAnalyzerConfig { KeyPositions = "256" }.CreateOptions());

        Assert.Equal(nameof(GPerfAnalyzerConfig.KeyPositions), below.ParamName);
        Assert.Equal(nameof(GPerfAnalyzerConfig.KeyPositions), above.ParamName);
    }

    [Fact]
    public void GPerfAnalyzerConfig_CreateOptions_RejectsMalformedKeyPositionTokens()
    {
        ArgumentException text = Assert.Throws<ArgumentException>(() => new GPerfAnalyzerConfig { KeyPositions = "abc" }.CreateOptions());
        ArgumentException openRange = Assert.Throws<ArgumentException>(() => new GPerfAnalyzerConfig { KeyPositions = "1-" }.CreateOptions());
        ArgumentException negative = Assert.Throws<ArgumentException>(() => new GPerfAnalyzerConfig { KeyPositions = "-1" }.CreateOptions());

        Assert.Equal(nameof(GPerfAnalyzerConfig.KeyPositions), text.ParamName);
        Assert.Equal(nameof(GPerfAnalyzerConfig.KeyPositions), openRange.ParamName);
        Assert.Equal(nameof(GPerfAnalyzerConfig.KeyPositions), negative.ParamName);
    }

    [Fact]
    public void GPerfAnalyzerConfig_CreateOptions_NormalizesValues()
    {
        GPerfAnalyzerOptions options = new GPerfAnalyzerConfig
        {
            Jump = 2,
            MaxPositions = 3,
            Random = true,
            SizeMultiple = 0.5
        }.CreateOptions();

        Assert.Equal(0, options.InitialAssociationValue);
        Assert.Equal(3, options.Jump);
        Assert.Equal(2, options.MaxConfiguredPosition);
        Assert.Equal(0, options.MultipleIterations);
        Assert.Equal(0.5, options.SizeMultiple);
    }

    [Fact]
    public void GPerfAnalyzerConfig_CreateOptions_RejectsInvalidOptions()
    {
        ArgumentOutOfRangeException sizeMultiple = Assert.Throws<ArgumentOutOfRangeException>(() => new GPerfAnalyzerConfig { SizeMultiple = double.NaN }.CreateOptions());
        ArgumentOutOfRangeException positiveInfinitySizeMultiple = Assert.Throws<ArgumentOutOfRangeException>(() => new GPerfAnalyzerConfig { SizeMultiple = double.PositiveInfinity }.CreateOptions());
        ArgumentOutOfRangeException negativeInfinitySizeMultiple = Assert.Throws<ArgumentOutOfRangeException>(() => new GPerfAnalyzerConfig { SizeMultiple = double.NegativeInfinity }.CreateOptions());
        ArgumentOutOfRangeException negativeSizeMultiple = Assert.Throws<ArgumentOutOfRangeException>(() => new GPerfAnalyzerConfig { SizeMultiple = -1.0 }.CreateOptions());
        ArgumentOutOfRangeException zeroSizeMultiple = Assert.Throws<ArgumentOutOfRangeException>(() => new GPerfAnalyzerConfig { SizeMultiple = 0.0 }.CreateOptions());
        ArgumentOutOfRangeException iterations = Assert.Throws<ArgumentOutOfRangeException>(() => new GPerfAnalyzerConfig { MultipleIterations = -1 }.CreateOptions());
        ArgumentOutOfRangeException initialValue = Assert.Throws<ArgumentOutOfRangeException>(() => new GPerfAnalyzerConfig { InitialAssociationValue = -1 }.CreateOptions());
        ArgumentOutOfRangeException jump = Assert.Throws<ArgumentOutOfRangeException>(() => new GPerfAnalyzerConfig { Jump = -1 }.CreateOptions());
        ArgumentOutOfRangeException maxPositionsZero = Assert.Throws<ArgumentOutOfRangeException>(() => new GPerfAnalyzerConfig { MaxPositions = 0 }.CreateOptions());
        ArgumentOutOfRangeException maxPositionsTooHigh = Assert.Throws<ArgumentOutOfRangeException>(() => new GPerfAnalyzerConfig { MaxPositions = 256 }.CreateOptions());

        Assert.Equal(nameof(GPerfAnalyzerConfig.SizeMultiple), sizeMultiple.ParamName);
        Assert.Equal(nameof(GPerfAnalyzerConfig.SizeMultiple), positiveInfinitySizeMultiple.ParamName);
        Assert.Equal(nameof(GPerfAnalyzerConfig.SizeMultiple), negativeInfinitySizeMultiple.ParamName);
        Assert.Equal(nameof(GPerfAnalyzerConfig.SizeMultiple), negativeSizeMultiple.ParamName);
        Assert.Equal(nameof(GPerfAnalyzerConfig.SizeMultiple), zeroSizeMultiple.ParamName);
        Assert.Equal(nameof(GPerfAnalyzerConfig.MultipleIterations), iterations.ParamName);
        Assert.Equal(nameof(GPerfAnalyzerConfig.InitialAssociationValue), initialValue.ParamName);
        Assert.Equal(nameof(GPerfAnalyzerConfig.Jump), jump.ParamName);
        Assert.Equal(nameof(GPerfAnalyzerConfig.MaxPositions), maxPositionsZero.ParamName);
        Assert.Equal(nameof(GPerfAnalyzerConfig.MaxPositions), maxPositionsTooHigh.ParamName);
    }

    [Fact]
    public void GetCandidates_MultipleIterations_GeneratesUsableHash()
    {
        string[] data = ["abc", "acb", "bca", "cab"];
        GPerfAnalyzerConfig config = new GPerfAnalyzerConfig { MultipleIterations = 5 };
        Candidate candidate = GetCandidate(data, false, GeneratorEncoding.AsciiBytes, config);
        StringHashFunc func = candidate.StringHash.GetExpression().Compile();

        AssertRawHashesUnique("multiple iterations", data, func, GeneratorEncoding.AsciiBytes);
    }

    [Fact]
    public void GetCandidates_RandomAssociationSearch_GeneratesUsableHash()
    {
        string[] data = ["az", "za", "ay", "ya"];
        GPerfAnalyzerConfig config = new GPerfAnalyzerConfig { Random = true, RandomSeed = 123, Jump = 0 };
        Candidate candidate = GetCandidate(data, false, GeneratorEncoding.AsciiBytes, config);
        StringHashFunc func = candidate.StringHash.GetExpression().Compile();

        AssertRawHashesUnique("random association search", data, func, GeneratorEncoding.AsciiBytes);
    }

    [Fact]
    public void GetCandidates_RandomSupersedesInitialAssociationValue()
    {
        string[] data = ["az", "za", "ay", "ya"];
        GPerfAnalyzerConfig leftConfig = new GPerfAnalyzerConfig { Random = true, RandomSeed = 123, InitialAssociationValue = 0 };
        GPerfAnalyzerConfig rightConfig = new GPerfAnalyzerConfig { Random = true, RandomSeed = 123, InitialAssociationValue = 37 };
        GPerfStringHash left = Assert.IsType<GPerfStringHash>(GetCandidate(data, false, GeneratorEncoding.AsciiBytes, leftConfig).StringHash);
        GPerfStringHash right = Assert.IsType<GPerfStringHash>(GetCandidate(data, false, GeneratorEncoding.AsciiBytes, rightConfig).StringHash);

        Assert.Equal(left.AssociationValues, right.AssociationValues);
    }

    [Fact]
    public void GetCandidates_MultipleIterationsUsesGeneratedInitialJumpSequence()
    {
        string[] data = ["abc", "acb", "bca", "cab"];
        GPerfAnalyzerConfig leftConfig = new GPerfAnalyzerConfig { MultipleIterations = 4 };
        GPerfAnalyzerConfig rightConfig = new GPerfAnalyzerConfig { MultipleIterations = 4, InitialAssociationValue = 11, Jump = 13, Random = true, RandomSeed = 123 };
        GPerfStringHash left = Assert.IsType<GPerfStringHash>(GetCandidate(data, false, GeneratorEncoding.AsciiBytes, leftConfig).StringHash);
        GPerfStringHash right = Assert.IsType<GPerfStringHash>(GetCandidate(data, false, GeneratorEncoding.AsciiBytes, rightConfig).StringHash);

        Assert.Equal(left.AssociationValues, right.AssociationValues);
    }

    [Fact]
    public void GetCandidates_Utf8Input_UsesEncodedBytes()
    {
        string[] data = ["\u010desky", "Dansk", "Fran\u00e7ais", "\u65e5\u672c\u8a9e", "\ud55c\uae00"];
        Candidate candidate = GetCandidate(data, false, GeneratorEncoding.Utf8Bytes);
        GPerfStringHash hash = Assert.IsType<GPerfStringHash>(candidate.StringHash);
        StringHashFunc func = candidate.StringHash.GetExpression().Compile();

        LengthLessThanEarlyExit exit = Assert.IsType<LengthLessThanEarlyExit>(Assert.Single(hash.GetMandatoryExits()));
        Assert.Equal(5, exit.Value);
        AssertRawHashesUnique("utf8 input", data, func, GeneratorEncoding.Utf8Bytes);
    }

    [Fact]
    public void GetCandidates_AsciiBytes_ReturnsLengthAndAsciiOnlyMandatoryExits()
    {
        string[] data = ["a", "bb", "ccc"];
        Candidate candidate = GetCandidate(data, false, GeneratorEncoding.AsciiBytes);
        GPerfStringHash hash = Assert.IsType<GPerfStringHash>(candidate.StringHash);
        IEarlyExit[] exits = hash.GetMandatoryExits().ToArray();

        LengthLessThanEarlyExit length = Assert.IsType<LengthLessThanEarlyExit>(exits[0]);
        Assert.Equal(1, length.Value);
        Assert.IsType<IsAsciiOnlyEarlyExit>(exits[1]);
    }

    [Fact]
    public void GetCandidates_SevenBit_ReturnsLengthAndAsciiOnlyMandatoryExits()
    {
        string[] data = ["a", "bb", "ccc"];
        GPerfAnalyzerConfig config = new GPerfAnalyzerConfig { SevenBit = true };
        Candidate candidate = GetCandidate(data, false, GeneratorEncoding.Utf8Bytes, config);
        GPerfStringHash hash = Assert.IsType<GPerfStringHash>(candidate.StringHash);
        IEarlyExit[] exits = hash.GetMandatoryExits().ToArray();

        LengthLessThanEarlyExit length = Assert.IsType<LengthLessThanEarlyExit>(exits[0]);
        Assert.Equal(1, length.Value);
        Assert.IsType<IsAsciiOnlyEarlyExit>(exits[1]);
    }

    [Fact]
    public void GetCandidates_MaxPositionsBelowMandatoryPosition_KeepsMandatoryPosition()
    {
        string[] data = ["aaXaa", "aaYaa"];
        GPerfAnalyzerConfig config = new GPerfAnalyzerConfig { MaxPositions = 2, NoLength = true };
        Candidate candidate = GetCandidate(data, false, GeneratorEncoding.AsciiBytes, config);
        GPerfStringHash hash = Assert.IsType<GPerfStringHash>(candidate.StringHash);
        StringHashFunc func = hash.GetExpression().Compile();

        Assert.Equal(new[] { 2 }, hash.Positions);
        AssertRawHashesUnique("mandatory position outside max positions", data, func, GeneratorEncoding.AsciiBytes);
    }

    [Fact]
    public void GetCandidates_NulBytes_GeneratesUsableHash()
    {
        string[] data = ["a\0x", "a\0y"];
        GPerfAnalyzerConfig config = new GPerfAnalyzerConfig { KeyPositions = "3", NoLength = true };
        Candidate candidate = GetCandidate(data, false, GeneratorEncoding.Utf8Bytes, config);
        StringHashFunc func = candidate.StringHash.GetExpression().Compile();

        AssertRawHashesUnique("nul byte input", data, func, GeneratorEncoding.Utf8Bytes);
    }

    [Fact]
    public void GetCandidates_ReusedAnalyzer_DoesNotLeakInternalState()
    {
        string[] data = GetSmtpHeaderFields();
        StringKeyProperties props = KeyAnalyzer.GetStringProperties(data, true, GeneratorEncoding.AsciiBytes);
        Simulator sim = new Simulator(data.Length, GeneratorEncoding.AsciiBytes);
        GPerfAnalyzer analyzer = new GPerfAnalyzer(data.Length, props, new GPerfAnalyzerConfig(), sim, NullLogger<GPerfAnalyzer>.Instance, GeneratorEncoding.AsciiBytes, true);

        Assert.True(analyzer.IsAppropriate());

        for (int run = 0; run < 2; run++)
        {
            Candidate candidate = Assert.Single(analyzer.GetCandidates(data));
            StringHashFunc func = candidate.StringHash.GetExpression().Compile();
            AssertRawHashesUnique($"smtp.gperf run {run}", data, func, GeneratorEncoding.AsciiBytes);
        }
    }

    [Fact]
    public void IsAppropriate_RejectsOnlyUnsupportedAsciiByteInput()
    {
        AssertInappropriate(["\u00e9a", "\u00e9b"], false, GeneratorEncoding.AsciiBytes);
        AssertInappropriate(["\u00e9a", "\u00c9b"], true, GeneratorEncoding.Utf8Bytes);
        AssertAppropriate(["\u00e9a", "\u00e9b"], false, GeneratorEncoding.Utf8Bytes, new GPerfAnalyzerConfig());

        static void AssertAppropriate(string[] data, bool ignoreCase, GeneratorEncoding encoding, GPerfAnalyzerConfig config)
        {
            StringKeyProperties props = KeyAnalyzer.GetStringProperties(data, ignoreCase, encoding);
            Simulator sim = new Simulator(data.Length, encoding);
            GPerfAnalyzer analyzer = new GPerfAnalyzer(data.Length, props, config, sim, NullLogger<GPerfAnalyzer>.Instance, encoding, ignoreCase);

            Assert.True(analyzer.IsAppropriate());
        }

        static void AssertInappropriate(string[] data, bool ignoreCase, GeneratorEncoding encoding)
        {
            StringKeyProperties props = KeyAnalyzer.GetStringProperties(data, ignoreCase, encoding);
            Simulator sim = new Simulator(data.Length, encoding);
            GPerfAnalyzer analyzer = new GPerfAnalyzer(data.Length, props, new GPerfAnalyzerConfig(), sim, NullLogger<GPerfAnalyzer>.Instance, encoding, ignoreCase);

            Assert.False(analyzer.IsAppropriate());
        }
    }

    [Fact]
    public void GetCandidates_SevenBitRejectsNonAsciiBytes()
    {
        string[] data = ["\u00e9a", "\u00e9b"];
        GPerfAnalyzerConfig config = new GPerfAnalyzerConfig { SevenBit = true };
        StringKeyProperties props = KeyAnalyzer.GetStringProperties(data, false, GeneratorEncoding.Utf8Bytes);
        Simulator sim = new Simulator(data.Length, GeneratorEncoding.Utf8Bytes);
        GPerfAnalyzer analyzer = new GPerfAnalyzer(data.Length, props, config, sim, NullLogger<GPerfAnalyzer>.Instance, GeneratorEncoding.Utf8Bytes, false);

        Assert.True(analyzer.IsAppropriate());
        Assert.Empty(analyzer.GetCandidates(data));
    }

    private static Candidate GetCandidate(string[] data, bool ignoreCase, GeneratorEncoding encoding = GeneratorEncoding.AsciiBytes, GPerfAnalyzerConfig? config = null)
    {
        StringKeyProperties props = KeyAnalyzer.GetStringProperties(data, ignoreCase, encoding);
        Simulator sim = new Simulator(data.Length, encoding);
        GPerfAnalyzer analyzer = new GPerfAnalyzer(data.Length, props, config ?? new GPerfAnalyzerConfig(), sim, NullLogger<GPerfAnalyzer>.Instance, encoding, ignoreCase);

        Assert.True(analyzer.IsAppropriate());
        return Assert.Single(analyzer.GetCandidates(data));
    }

    private static HashTableContext<string, byte> CreateHashTable(string[] data, StringHashFunc func, GeneratorEncoding encoding)
    {
        Func<string, byte[]> getBytes = StringHelper.GetBytesFunc(encoding);
        HashData hashData = HashData.Create(data, 1, x =>
        {
            byte[] bytes = getBytes(x);
            return func(bytes, bytes.Length);
        });

        HashTableStructure<string, byte> structure = new HashTableStructure<string, byte>(hashData);
        return structure.Create(data, ReadOnlyMemory<byte>.Empty);
    }

    private static bool Contains(HashTableContext<string, byte> context, StringHashFunc func, string key, StringComparer comparer, GeneratorEncoding encoding)
    {
        byte[] bytes = StringHelper.GetBytesFunc(encoding)(key);
        ulong hash = func(bytes, bytes.Length);
        int entryIndex = context.Buckets[hash % (ulong)context.Buckets.Length] - 1;

        while (entryIndex >= 0)
        {
            HashTableEntry<string> entry = context.Entries[entryIndex];
            if (entry.Hash == hash && comparer.Equals(entry.Key, key))
                return true;

            entryIndex = entry.Next;
        }

        return false;
    }

    private static void AssertRawHashesUnique(string name, string[] data, StringHashFunc func, GeneratorEncoding encoding)
    {
        Func<string, byte[]> getBytes = StringHelper.GetBytesFunc(encoding);
        HashSet<ulong> hashes = new HashSet<ulong>();

        foreach (string key in data)
        {
            byte[] bytes = getBytes(key);
            Assert.True(hashes.Add(func(bytes, bytes.Length)), $"{name} produced a duplicate raw hash for {key}");
        }
    }

    private static string[] GetSmtpHeaderFields() =>
    [
        "Accept-Language", "Action", "Alternate-Recipient", "Approved", "Archive", "Arrival-Date", "Autoforwarded", "Autosubmitted",
        "Bcc", "Cc", "Comments", "Complaints-To", "Content-alternative", "Content-Base", "Content-Description", "Content-Disposition",
        "Content-Duration", "Content-Features", "Content-ID", "Content-Language", "Content-Location", "Content-MD5", "Content-Transfer-Encoding",
        "Content-Type", "Control", "Conversion", "Conversion-With-Loss", "DL-Expansion-History", "DSN-Gateway", "Date", "Deferred-Delivery",
        "Delivery-Date", "Diagnostic-Code", "Discarded-X400-IPMS-Extensions", "Discarded-X400-MTS-Extensions", "Disclose-Recipients", "Disposition",
        "Disposition-Notification-Options", "Disposition-Notification-To", "Distribution", "Encrypted", "Error", "Expires", "Failure", "Final-Log-ID",
        "Final-Recipient", "Followup-To", "From", "Generate-Delivery-Report", "Importance", "In-Reply-To", "Incomplete-Copy", "Injector-Info",
        "Keywords", "Last-Attempt-Date", "Latest-Delivery-Time", "Lines", "List-Archive", "List-Help", "List-ID", "List-Post", "List-Owner",
        "List-Subscribe", "List-Unsubscribe", "MDN-Gateway", "Media-Accept-Features", "MIME-Version", "Mail-Copies-To", "Message-ID",
        "Message-Type", "Newsgroups", "Organization", "Original-Encoded-Information-Types", "Original-Envelope-ID", "Original-Message-ID",
        "Original-Recipient", "Originator-Return-Address", "Path", "Posted-And-Mailed", "Prevent-Nondelivery-Report", "Priority", "Received",
        "Received-content-MIC", "Received-From-MTA", "References", "Remote-MTA", "Reply-By", "Reply-To", "Reporting-MTA", "Reporting-UA",
        "Return-Path", "Sender", "Sensitivity", "Status", "Subject", "Summary", "Supersedes", "To", "User-Agent", "Warning", "Will-Retry-Until",
        "X400-Content-Identifier", "X400-Content-Return", "X400-Content-Type", "X400-MTS-Identifier", "X400-Originator", "X400-Received",
        "X400-Recipients", "Xref"
    ];
}