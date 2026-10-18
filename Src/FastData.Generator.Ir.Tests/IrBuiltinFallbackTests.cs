using System.Numerics;
using System.Text;
using Genbox.FastData.Generator.Ir.Building;
using Genbox.FastData.Generator.Ir.Operations;
using Genbox.FastData.Generator.Ir.Tests.Code;
using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;
using static Genbox.FastData.Ir.TestSupport.IrTestPrograms;

namespace Genbox.FastData.Generator.Ir.Tests;

/// <summary>Native builtins and programs built with only the required primitives must match independent reference values.</summary>
public class IrBuiltinFallbackTests
{
    private static readonly IrTextEncoding[] _encodings = [IrTextEncoding.Utf8, IrTextEncoding.Utf16];

    private static readonly ulong[] _specialValues = [0, 1, 2, 3, 0xFFFFFFFF, 0x100000000, 0x8000000000000000, 0xAAAAAAAAAAAAAAAA, 0x5555555555555555, ulong.MaxValue];

    public static TheoryData<IrBuiltinOperation, ulong, IrLiteral> BitCounts
    {
        get
        {
            Random random = new Random(1234);
            HashSet<ulong> values = new HashSet<ulong>(_specialValues);

            for (int bit = 0; bit < 64; bit++)
                values.Add(1UL << bit);

            for (int i = 0; i < 32; i++)
                values.Add(NextU64(random));

            TheoryData<IrBuiltinOperation, ulong, IrLiteral> data = new TheoryData<IrBuiltinOperation, ulong, IrLiteral>
            {
                { IrBuiltinOperation.PopCount, 0xA5, Constant(4) }
            };

            foreach (ulong value in values)
            {
                data.Add(IrBuiltinOperation.PopCount, value, Constant(BitOperations.PopCount(value)));
                data.Add(IrBuiltinOperation.TrailingZeroCount, value, Constant(BitOperations.TrailingZeroCount(value)));
            }

            return data;
        }
    }

    public static TheoryData<ulong, ulong, IrLiteral> MultiplyHighPairs
    {
        get
        {
            Random random = new Random(1234);
            TheoryData<ulong, ulong, IrLiteral> data = new TheoryData<ulong, ulong, IrLiteral>();

            // Special pairs cover zero, one, maxima, and carries across the 32-bit limbs.
            foreach (ulong left in _specialValues)
            {
                foreach (ulong right in _specialValues)
                    data.Add(left, right, Constant((ulong)((new BigInteger(left) * right) >> 64)));
            }

            for (int i = 0; i < 32; i++)
            {
                ulong left = NextU64(random);
                ulong right = NextU64(random);
                data.Add(left, right, Constant((ulong)((new BigInteger(left) * right) >> 64)));
            }

            return data;
        }
    }

    public static TheoryData<IrTextEncoding, string, string, string> Regions
    {
        get
        {
            TheoryData<IrTextEncoding, string, string, string> data = new TheoryData<IrTextEncoding, string, string, string>();

            foreach (IrTextEncoding encoding in _encodings)
            {
                data.Add(encoding, "abc", "", "ab");
                data.Add(encoding, "abc", "a", "bc");
                data.Add(encoding, "abc", "a", "bd");
                data.Add(encoding, "abc", "abc", "");
                data.Add(encoding, "", "", "");
                data.Add(encoding, "aé😀", "a", "é");
                data.Add(encoding, "aé😀", "a", "è");
                data.Add(encoding, "aé😀", "aé", "😀");
                data.Add(encoding, "aé😀", "aé", "😁");
            }

            return data;
        }
    }

    public static TheoryData<IrTextEncoding, string> AsciiTexts
    {
        get
        {
            TheoryData<IrTextEncoding, string> data = new TheoryData<IrTextEncoding, string>();

            foreach (IrTextEncoding encoding in _encodings)
            {
                foreach (string text in new[] { "", "abc", "\u007F", "\u0080", "é", "a😀" })
                    data.Add(encoding, text);
            }

            return data;
        }
    }

    public static TheoryData<IrTextEncoding, IrBuiltinOperation, string, int, IrLiteral> ByteReads
    {
        get
        {
            (IrBuiltinOperation Operation, int Size)[] reads =
            [
                (IrBuiltinOperation.ReadU8, 1), (IrBuiltinOperation.ReadU16LittleEndian, 2),
                (IrBuiltinOperation.ReadU32LittleEndian, 4), (IrBuiltinOperation.ReadU64LittleEndian, 8)
            ];
            TheoryData<IrTextEncoding, IrBuiltinOperation, string, int, IrLiteral> data = new TheoryData<IrTextEncoding, IrBuiltinOperation, string, int, IrLiteral>();

            foreach (IrTextEncoding encoding in _encodings)
            {
                // Unpaired surrogates exist only as UTF-16 units.
                string[] texts = encoding != IrTextEncoding.Utf16 ? ["héllo😀wörld", "\u0001ÿĀ￿"] : ["héllo😀wörld", "\u0001ÿĀ￿", "\uD800ab"];

                foreach (string text in texts)
                {
                    byte[] bytes = EncodeBytes(encoding, text);

                    foreach ((IrBuiltinOperation operation, int size) in reads)
                    {
                        for (int offset = 0; offset + size <= bytes.Length; offset++)
                        {
                            ulong value = 0;
                            for (int k = size - 1; k >= 0; k--)
                                value = (value << 8) | bytes[offset + k];
                            data.Add(encoding, operation, text, offset, size == 8 ? Constant(value) : Constant((uint)value));
                        }
                    }
                }

                // Both texts encode the bytes 00 01 02 ... 08, read from byte offset 1.
                string unaligned = encoding == IrTextEncoding.Utf8 ? "\0\u0001\u0002\u0003\u0004\u0005\u0006\u0007\u0008" : "Ā̂Ԅ܆\u0008";
                data.Add(encoding, IrBuiltinOperation.ReadU8, unaligned, 1, Constant(0x01u));
                data.Add(encoding, IrBuiltinOperation.ReadU16LittleEndian, unaligned, 1, Constant(0x0201u));
                data.Add(encoding, IrBuiltinOperation.ReadU32LittleEndian, unaligned, 1, Constant(0x04030201u));
                data.Add(encoding, IrBuiltinOperation.ReadU64LittleEndian, unaligned, 1, Constant(0x0807060504030201UL));
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(BitCounts))]
    public void BitCountsMatchReference(IrBuiltinOperation operation, ulong value, IrLiteral expected) => AssertNativeAndFallback(IrTextEncoding.Utf16, operation, expected, Constant(value));

    [Theory]
    [MemberData(nameof(MultiplyHighPairs))]
    public void MultiplyHighMatchesReference(ulong left, ulong right, IrLiteral expected) => AssertNativeAndFallback(IrTextEncoding.Utf16, IrBuiltinOperation.MultiplyHigh, expected, Constant(left), Constant(right));

    [Theory]
    [MemberData(nameof(Regions))]
    public void TextEqualsAtComparesUnits(IrTextEncoding encoding, string text, string prefix, string fragment)
    {
        int offset = new IrText(encoding).GetLength(prefix);
        bool expected = text.AsSpan(prefix.Length).StartsWith(fragment.AsSpan(), StringComparison.Ordinal);
        AssertNativeAndFallback(encoding, IrBuiltinOperation.TextEqualsAt, Constant(expected), Constant(text), Constant(offset), Constant(fragment));
    }

    [Theory]
    [MemberData(nameof(AsciiTexts))]
    public void TextIsAsciiScansUnits(IrTextEncoding encoding, string text) => AssertNativeAndFallback(encoding, IrBuiltinOperation.TextIsAscii, Constant(text.All(static c => c < 0x80)), Constant(text));

    [Theory]
    [MemberData(nameof(ByteReads))]
    public void ByteReadsUseLittleEndian(IrTextEncoding encoding, IrBuiltinOperation operation, string text, int offset, IrLiteral expected) => AssertNativeAndFallback(encoding, operation, expected, Constant(text), Constant(offset));

    [Fact]
    public void DeclaresFallbacksOnce()
    {
        IrProgramBuilder program = new IrProgramBuilder();
        IrBuiltinBuilder builtins = new IrBuiltinBuilder(program, IrBuiltins.Required);

        IrBuiltinOperation[] operations = Enum.GetValues<IrBuiltinOperation>().Where(static operation => operation != IrBuiltinOperation.None).ToArray();
        foreach (IrBuiltinOperation operation in operations)
            Assert.Equal(builtins.Declare(operation), builtins.Declare(operation));

        IrProgram built = program.Build();
        string[] expectedSymbols = [IrBuiltins.GetSymbol(IrBuiltinOperation.TextLength), IrBuiltins.GetSymbol(IrBuiltinOperation.TextUnitAt)];
        IEnumerable<string> actualSymbols = built.Callables.OfType<IrExternalFunction>().Select(static function => function.Symbol).Order(StringComparer.Ordinal);
        Assert.Equal(expectedSymbols, actualSymbols, StringComparer.Ordinal);
        Assert.Equal(operations.Length - 2, built.Callables.OfType<IrFunction>().Count());
    }

    [Fact]
    public void BitOperationsNeedNoExternals()
    {
        IrProgramBuilder program = new IrProgramBuilder();
        IrBuiltinBuilder builtins = new IrBuiltinBuilder(program, IrBuiltins.Required);
        builtins.Declare(IrBuiltinOperation.PopCount);
        builtins.Declare(IrBuiltinOperation.TrailingZeroCount);
        builtins.Declare(IrBuiltinOperation.MultiplyHigh);
        Assert.Empty(program.Build().Callables.OfType<IrExternalFunction>());
    }

    private static void AssertNativeAndFallback(IrTextEncoding encoding, IrBuiltinOperation operation, IrLiteral expected, params IrExpression[] arguments)
    {
        Assert.Equal(expected, LookupTestPrograms.Evaluate(IrType.I32, expected.Type, x => x.Builtins.Call(operation, arguments), encoding));
        Assert.Equal(expected, LookupTestPrograms.Evaluate(IrType.I32, expected.Type, x => x.Builtins.Call(operation, arguments), encoding, supportedBuiltins: IrBuiltins.Required));
    }

    private static byte[] EncodeBytes(IrTextEncoding encoding, string text)
    {
        if (encoding == IrTextEncoding.Utf8)
            return Encoding.UTF8.GetBytes(text);

        // Encode UTF-16 units directly so unpaired surrogates survive.
        byte[] bytes = new byte[text.Length * 2];

        for (int i = 0; i < text.Length; i++)
        {
            bytes[2 * i] = (byte)(text[i] & 0xFF);
            bytes[(2 * i) + 1] = (byte)(text[i] >> 8);
        }

        return bytes;
    }

    private static ulong NextU64(Random random) => ((ulong)(uint)random.Next() << 33) ^ ((ulong)(uint)random.Next() << 11) ^ (uint)random.Next();
}