using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Genbox.FastData.Internal.Encodings;

namespace Genbox.FastData.Tests;

[SuppressMessage("Usage", "xUnit1016:MemberData must reference a public member")]
public class IntegerEncodingTests(ITestOutputHelper output)
{
    private static readonly IIntegerEncoding[] Encodings =
    [
        Leb128Encoding.Instance,
        VlqEncoding.Instance,
        QuicEncoding.Instance,
        DlugoszEncoding.Instance,
        SqliteEncoding.Instance,
        CborEncoding.Instance,
        Bijou64Encoding.Instance,
        Varu64Encoding.Instance,
        Vu128Encoding.Instance,
        PrefixEncoding.Instance,
        GitPackEncoding.Instance
    ];

    private static readonly ulong[] RoundTripValues =
    [
        0,
        1,
        10,
        23,
        24,
        42,
        63,
        64,
        127,
        128,
        239,
        240,
        247,
        248,
        255,
        256,
        300,
        503,
        504,
        1_000,
        16_383,
        16_384,
        65_535,
        66_040,
        67_000,
        2_097_151,
        2_097_152,
        268_435_455,
        268_435_456,
        uint.MaxValue,
        0x03ffffffff,
        0x0400000000,
        4_311_810_552,
        0x00ffffffffffff,
        0x01000000000000,
        72_340_172_838_076_920,
        QuicEncoding.MaxValue,
        ulong.MaxValue
    ];

    internal static TheoryData<IIntegerEncoding, ulong, string> GetKnownVectors() => new TheoryData<IIntegerEncoding, ulong, string>
    {
        // LEB128 / Protocol Buffers unsigned base-128 varint examples.
        { Leb128Encoding.Instance, 0, "00" },
        { Leb128Encoding.Instance, 1, "01" },
        { Leb128Encoding.Instance, 127, "7F" },
        { Leb128Encoding.Instance, 128, "8001" },
        { Leb128Encoding.Instance, 150, "9601" },
        { Leb128Encoding.Instance, 16_383, "FF7F" },
        { Leb128Encoding.Instance, 16_384, "808001" },
        { Leb128Encoding.Instance, 2_097_151, "FFFF7F" },
        { Leb128Encoding.Instance, 2_097_152, "80808001" },
        { Leb128Encoding.Instance, 624_485, "E58E26" },
        { Leb128Encoding.Instance, ulong.MaxValue, "FFFFFFFFFFFFFFFFFF01" },

        // MIDI-style big-endian VLQ examples.
        { VlqEncoding.Instance, 0, "00" },
        { VlqEncoding.Instance, 64, "40" },
        { VlqEncoding.Instance, 127, "7F" },
        { VlqEncoding.Instance, 128, "8100" },
        { VlqEncoding.Instance, 256, "8200" },
        { VlqEncoding.Instance, 16_383, "FF7F" },
        { VlqEncoding.Instance, 16_384, "818000" },
        { VlqEncoding.Instance, 2_097_151, "FFFF7F" },
        { VlqEncoding.Instance, 2_097_152, "81808000" },
        { VlqEncoding.Instance, ulong.MaxValue, "81FFFFFFFFFFFFFFFF7F" },

        // RFC 9000 section 16 QUIC examples and MsQuic boundary vectors.
        { QuicEncoding.Instance, 0, "00" },
        { QuicEncoding.Instance, 37, "25" },
        { QuicEncoding.Instance, 0x3F, "3F" },
        { QuicEncoding.Instance, 0x40, "4040" },
        { QuicEncoding.Instance, 15_293, "7BBD" },
        { QuicEncoding.Instance, 0x3FFF, "7FFF" },
        { QuicEncoding.Instance, 0x4000, "80004000" },
        { QuicEncoding.Instance, 494_878_333, "9D7F3E7D" },
        { QuicEncoding.Instance, 0x3FFFFFFF, "BFFFFFFF" },
        { QuicEncoding.Instance, 0x40000000, "C000000040000000" },
        { QuicEncoding.Instance, 151_288_809_941_952_652, "C2197C5EFF14E88C" },
        { QuicEncoding.Instance, QuicEncoding.MaxValue, "FFFFFFFFFFFFFFFF" },

        // Dlugosz Revision 2 examples and u64 boundary vectors.
        { DlugoszEncoding.Instance, 0, "00" },
        { DlugoszEncoding.Instance, 1, "01" },
        { DlugoszEncoding.Instance, 5, "05" },
        { DlugoszEncoding.Instance, 20, "14" },
        { DlugoszEncoding.Instance, 127, "7F" },
        { DlugoszEncoding.Instance, 128, "8080" },
        { DlugoszEncoding.Instance, 200, "80C8" },
        { DlugoszEncoding.Instance, 400, "8190" },
        { DlugoszEncoding.Instance, 10_000, "A710" },
        { DlugoszEncoding.Instance, 16_383, "BFFF" },
        { DlugoszEncoding.Instance, 16_384, "C04000" },
        { DlugoszEncoding.Instance, 2_000_000, "DE8480" },
        { DlugoszEncoding.Instance, 2_097_151, "DFFFFF" },
        { DlugoszEncoding.Instance, 2_097_152, "E0200000" },
        { DlugoszEncoding.Instance, 134_217_727, "E7FFFFFF" },
        { DlugoszEncoding.Instance, 134_217_728, "E808000000" },
        { DlugoszEncoding.Instance, 34_359_738_367, "EFFFFFFFFF" },
        { DlugoszEncoding.Instance, 34_359_738_368, "F80800000000" },
        { DlugoszEncoding.Instance, 1_099_511_627_775, "F8FFFFFFFFFF" },
        { DlugoszEncoding.Instance, 1_099_511_627_776, "F000010000000000" },
        { DlugoszEncoding.Instance, 0x07ffffffffffffff, "F7FFFFFFFFFFFFFF" },
        { DlugoszEncoding.Instance, 0x0800000000000000, "F90800000000000000" },
        { DlugoszEncoding.Instance, ulong.MaxValue, "F9FFFFFFFFFFFFFFFF" },

        // SQLite3 database file format varint examples derived from the published format.
        { SqliteEncoding.Instance, 0, "00" },
        { SqliteEncoding.Instance, 127, "7F" },
        { SqliteEncoding.Instance, 128, "8100" },
        { SqliteEncoding.Instance, 16_383, "FF7F" },
        { SqliteEncoding.Instance, 16_384, "818000" },
        { SqliteEncoding.Instance, 2_097_151, "FFFF7F" },
        { SqliteEncoding.Instance, 2_097_152, "81808000" },
        { SqliteEncoding.Instance, 268_435_455, "FFFFFF7F" },
        { SqliteEncoding.Instance, 268_435_456, "8180808000" },
        { SqliteEncoding.Instance, ulong.MaxValue, "FFFFFFFFFFFFFFFFFF" },

        // RFC 8949 CBOR unsigned integer examples.
        { CborEncoding.Instance, 0, "00" },
        { CborEncoding.Instance, 10, "0A" },
        { CborEncoding.Instance, 23, "17" },
        { CborEncoding.Instance, 24, "1818" },
        { CborEncoding.Instance, 1_000, "1903E8" },
        { CborEncoding.Instance, 255, "18FF" },
        { CborEncoding.Instance, 256, "190100" },
        { CborEncoding.Instance, 65_535, "19FFFF" },
        { CborEncoding.Instance, 65_536, "1A00010000" },
        { CborEncoding.Instance, uint.MaxValue, "1AFFFFFFFF" },
        { CborEncoding.Instance, 4_294_967_296, "1B0000000100000000" },
        { CborEncoding.Instance, ulong.MaxValue, "1BFFFFFFFFFFFFFFFF" },

        // Ink & Switch bijou64 specification test vectors.
        { Bijou64Encoding.Instance, 0, "00" },
        { Bijou64Encoding.Instance, 1, "01" },
        { Bijou64Encoding.Instance, 42, "2A" },
        { Bijou64Encoding.Instance, 247, "F7" },
        { Bijou64Encoding.Instance, 248, "F800" },
        { Bijou64Encoding.Instance, 300, "F834" },
        { Bijou64Encoding.Instance, 503, "F8FF" },
        { Bijou64Encoding.Instance, 504, "F90000" },
        { Bijou64Encoding.Instance, 1_000, "F901F0" },
        { Bijou64Encoding.Instance, 66_039, "F9FFFF" },
        { Bijou64Encoding.Instance, 66_040, "FA000000" },
        { Bijou64Encoding.Instance, 67_000, "FA0003C0" },
        { Bijou64Encoding.Instance, 16_843_255, "FAFFFFFF" },
        { Bijou64Encoding.Instance, 16_843_256, "FB00000000" },
        { Bijou64Encoding.Instance, 4_311_810_551, "FBFFFFFFFF" },
        { Bijou64Encoding.Instance, 4_311_810_552, "FC0000000000" },
        { Bijou64Encoding.Instance, 1_103_823_438_327, "FCFFFFFFFFFF" },
        { Bijou64Encoding.Instance, 1_103_823_438_328, "FD000000000000" },
        { Bijou64Encoding.Instance, 282_578_800_148_983, "FDFFFFFFFFFFFF" },
        { Bijou64Encoding.Instance, 282_578_800_148_984, "FE00000000000000" },
        { Bijou64Encoding.Instance, 72_340_172_838_076_919, "FEFFFFFFFFFFFFFF" },
        { Bijou64Encoding.Instance, 72_340_172_838_076_920, "FF0000000000000000" },
        { Bijou64Encoding.Instance, ulong.MaxValue, "FFFEFEFEFEFEFEFE07" },

        // VARU64 upstream fixtures.
        { Varu64Encoding.Instance, 0, "00" },
        { Varu64Encoding.Instance, 1, "01" },
        { Varu64Encoding.Instance, 247, "F7" },
        { Varu64Encoding.Instance, 248, "F8F8" },
        { Varu64Encoding.Instance, 255, "F8FF" },
        { Varu64Encoding.Instance, 256, "F90100" },
        { Varu64Encoding.Instance, 65_535, "F9FFFF" },
        { Varu64Encoding.Instance, 65_536, "FA010000" },
        { Varu64Encoding.Instance, 72_057_594_037_927_935, "FEFFFFFFFFFFFFFF" },
        { Varu64Encoding.Instance, 72_057_594_037_927_936, "FF0100000000000000" },

        // vu128 upstream u64 test vectors.
        { Vu128Encoding.Instance, 0, "00" },
        { Vu128Encoding.Instance, 0x7F, "7F" },
        { Vu128Encoding.Instance, 0xABCDE, "DEE655" },
        { Vu128Encoding.Instance, 0x80, "8002" },
        { Vu128Encoding.Instance, 0x3fff, "BFFF" },
        { Vu128Encoding.Instance, 0x4000, "C00002" },
        { Vu128Encoding.Instance, 0x1FFFFF, "DFFFFF" },
        { Vu128Encoding.Instance, 0x200000, "E0000002" },
        { Vu128Encoding.Instance, 0xFFFFFFF, "EFFFFFFF" },
        { Vu128Encoding.Instance, 0x10000000, "F300000010" },
        { Vu128Encoding.Instance, 0xFFFFFFFF, "F3FFFFFFFF" },
        { Vu128Encoding.Instance, 0x00000001_FFFFFFFF, "F4FFFFFFFF01" },
        { Vu128Encoding.Instance, 0x000000FF_FFFFFFFF, "F4FFFFFFFFFF" },
        { Vu128Encoding.Instance, 0x000001FF_FFFFFFFF, "F5FFFFFFFFFF01" },
        { Vu128Encoding.Instance, 0x0000FFFF_FFFFFFFF, "F5FFFFFFFFFFFF" },
        { Vu128Encoding.Instance, 0x0001FFFF_FFFFFFFF, "F6FFFFFFFFFFFF01" },
        { Vu128Encoding.Instance, 0x00FFFFFF_FFFFFFFF, "F6FFFFFFFFFFFFFF" },
        { Vu128Encoding.Instance, 0x01FFFFFF_FFFFFFFF, "F7FFFFFFFFFFFFFF01" },
        { Vu128Encoding.Instance, ulong.MaxValue, "F7FFFFFFFFFFFFFFFF" },

        // Chromium libtextclassifier PrefixVarInt boundary vectors.
        { PrefixEncoding.Instance, 0, "00" },
        { PrefixEncoding.Instance, 1, "01" },
        { PrefixEncoding.Instance, 127, "7F" },
        { PrefixEncoding.Instance, 128, "8002" },
        { PrefixEncoding.Instance, 16_383, "BFFF" },
        { PrefixEncoding.Instance, 16_384, "C00002" },
        { PrefixEncoding.Instance, 2_097_151, "DFFFFF" },
        { PrefixEncoding.Instance, 2_097_152, "E0000002" },
        { PrefixEncoding.Instance, 268_435_455, "EFFFFFFF" },
        { PrefixEncoding.Instance, 268_435_456, "F000000002" },
        { PrefixEncoding.Instance, 34_359_738_367, "F7FFFFFFFF" },
        { PrefixEncoding.Instance, 34_359_738_368, "F80000000002" },
        { PrefixEncoding.Instance, 4_398_046_511_103, "FBFFFFFFFFFF" },
        { PrefixEncoding.Instance, 4_398_046_511_104, "FC000000000002" },
        { PrefixEncoding.Instance, 562_949_953_421_311, "FDFFFFFFFFFFFF" },
        { PrefixEncoding.Instance, 562_949_953_421_312, "FE00000000000002" },
        { PrefixEncoding.Instance, 72_057_594_037_927_935, "FEFFFFFFFFFFFFFF" },
        { PrefixEncoding.Instance, 72_057_594_037_927_936, "FF0000000000000001" },
        { PrefixEncoding.Instance, ulong.MaxValue, "FFFFFFFFFFFFFFFFFF" },

        // Git pack-format offset encoding examples derived from the specification.
        { GitPackEncoding.Instance, 0, "00" },
        { GitPackEncoding.Instance, 1, "01" },
        { GitPackEncoding.Instance, 127, "7F" },
        { GitPackEncoding.Instance, 128, "8000" },
        { GitPackEncoding.Instance, 129, "8001" },
        { GitPackEncoding.Instance, 255, "807F" },
        { GitPackEncoding.Instance, 256, "8100" },
        { GitPackEncoding.Instance, 16_511, "FF7F" },
        { GitPackEncoding.Instance, 16_512, "808000" },
        { GitPackEncoding.Instance, 2_113_663, "FFFF7F" },
        { GitPackEncoding.Instance, 2_113_664, "80808000" }
    };

    internal static TheoryData<uint, string> GetVu128UInt32Vectors() => new TheoryData<uint, string>
    {
        { 0xABCDE, "DEE655" },
        { 0x00000000, "00" },
        { 0x0000007F, "7F" },
        { 0x00000080, "8002" },
        { 0x00003FFF, "BFFF" },
        { 0x00004000, "C00002" },
        { 0x001FFFFF, "DFFFFF" },
        { 0x00200000, "E0000002" },
        { 0x0FFFFFFF, "EFFFFFFF" },
        { 0x10000000, "F300000010" },
        { 0xFFFFFFFF, "F3FFFFFFFF" }
    };

    internal static TheoryData<uint, string> GetBijou32Vectors() => new TheoryData<uint, string>
    {
        { 0, "00" },
        { 1, "01" },
        { 42, "2A" },
        { 251, "FB" },
        { 252, "FC00" },
        { 300, "FC30" },
        { 507, "FCFF" },
        { 508, "FD0000" },
        { 65_535, "FDFE03" },
        { 66_043, "FDFFFF" },
        { 66_044, "FE000000" },
        { 16_843_259, "FEFFFFFF" },
        { 16_843_260, "FF00000000" },
        { uint.MaxValue, "FFFEFEFE03" }
    };

    internal static TheoryData<float, string> GetVu128SingleVectors() => new TheoryData<float, string>
    {
        { 0.0f, "00" },
        { -0.0f, "8002" }
    };

    internal static TheoryData<double, string> GetVu128DoubleVectors() => new TheoryData<double, string>
    {
        { 0.0, "00" },
        { -0.0, "8002" }
    };

    [Theory]
    [MemberData(nameof(GetKnownVectors))]
    internal void KnownVectorMatches(IIntegerEncoding encoding, ulong value, string expectedHex)
    {
        Span<byte> buffer = stackalloc byte[encoding.MaxEncodedLength];
        int length = encoding.Encode(value, buffer);

        Assert.Equal(expectedHex, ToHex(buffer[..length]));
        Assert.True(encoding.TryDecode(buffer[..length], out ulong decoded, out int bytesRead));
        Assert.Equal(value, decoded);
        Assert.Equal(length, bytesRead);
    }

    [Theory]
    [MemberData(nameof(GetBijou32Vectors))]
    internal void Bijou32KnownVectorMatches(uint value, string expectedHex)
    {
        Span<byte> buffer = stackalloc byte[Bijou32Encoding.Instance.MaxEncodedLength];
        int length = Bijou32Encoding.Instance.Encode(value, buffer);

        Assert.Equal(expectedHex, ToHex(buffer[..length]));
        Assert.Equal(Bijou32Encoding.Instance.GetEncodedLength(value), length);
        Assert.True(Bijou32Encoding.Instance.TryDecode(buffer[..length], out uint decoded, out int bytesRead));
        Assert.Equal(value, decoded);
        Assert.Equal(length, bytesRead);
    }

    [Theory]
    [MemberData(nameof(GetVu128UInt32Vectors))]
    internal void Vu128UInt32KnownVectorMatches(uint value, string expectedHex)
    {
        Span<byte> buffer = stackalloc byte[Vu128Encoding.MaxUInt32EncodedLength];
        int length = Vu128Encoding.Instance.Encode(value, buffer);

        Assert.Equal(expectedHex, ToHex(buffer[..length]));
        Assert.Equal(Vu128Encoding.Instance.GetEncodedLength(value), length);
        Assert.True(Vu128Encoding.Instance.TryDecode(buffer[..length], out uint decoded, out int bytesRead));
        Assert.Equal(value, decoded);
        Assert.Equal(length, bytesRead);
    }

    [Fact]
    internal void Vu128UInt32DecodeIgnoresBytesAfterDeclaredBinaryPayload()
    {
        Assert.True(Vu128Encoding.Instance.TryDecode(FromHex("F101020304"), out uint decoded, out int bytesRead));
        Assert.Equal(0x0201U, decoded);
        Assert.Equal(3, bytesRead);
    }

    [Fact]
    internal void Vu128EncodeDoesNotOverwriteAfterReturnedLength()
    {
        Span<byte> buffer = stackalloc byte[Vu128Encoding.Instance.MaxEncodedLength];
        buffer.Fill(0xcc);

        int length = Vu128Encoding.Instance.Encode(0x10000000UL, buffer);

        Assert.Equal(5, length);
        for (int i = length; i < buffer.Length; i++)
            Assert.Equal(0xcc, buffer[i]);
    }

    [Theory]
    [MemberData(nameof(GetVu128SingleVectors))]
    internal void Vu128SingleKnownVectorMatches(float value, string expectedHex)
    {
        Span<byte> buffer = stackalloc byte[Vu128Encoding.MaxUInt32EncodedLength];
        int length = Vu128Encoding.Instance.Encode(value, buffer);

        Assert.Equal(expectedHex, ToHex(buffer[..length]));
        Assert.Equal(Vu128Encoding.Instance.GetEncodedLength(value), length);
        Assert.True(Vu128Encoding.Instance.TryDecode(buffer[..length], out float decoded, out int bytesRead));
        Assert.Equal(BitConverter.SingleToInt32Bits(value), BitConverter.SingleToInt32Bits(decoded));
        Assert.Equal(length, bytesRead);
    }

    [Theory]
    [MemberData(nameof(GetVu128DoubleVectors))]
    internal void Vu128DoubleKnownVectorMatches(double value, string expectedHex)
    {
        Span<byte> buffer = stackalloc byte[Vu128Encoding.Instance.MaxEncodedLength];
        int length = Vu128Encoding.Instance.Encode(value, buffer);

        Assert.Equal(expectedHex, ToHex(buffer[..length]));
        Assert.Equal(Vu128Encoding.Instance.GetEncodedLength(value), length);
        Assert.True(Vu128Encoding.Instance.TryDecode(buffer[..length], out double decoded, out int bytesRead));
        Assert.Equal(BitConverter.DoubleToInt64Bits(value), BitConverter.DoubleToInt64Bits(decoded));
        Assert.Equal(length, bytesRead);
    }

    [Fact]
    public void AllEncodingsRoundTripBoundaryValues()
    {
        foreach (IIntegerEncoding encoding in Encodings)
        {
            Span<byte> buffer = stackalloc byte[encoding.MaxEncodedLength];
            foreach (ulong value in RoundTripValues)
            {
                if (encoding == QuicEncoding.Instance && value > QuicEncoding.MaxValue)
                    continue;

                int length = encoding.Encode(value, buffer);
                Assert.Equal(encoding.GetEncodedLength(value), length);
                Assert.True(encoding.TryDecode(buffer[..length], out ulong decoded, out int bytesRead), encoding.GetType().Name + " failed to decode " + value.ToString(NumberFormatInfo.InvariantInfo));
                Assert.Equal(value, decoded);
                Assert.Equal(length, bytesRead);
            }
        }
    }

    [Fact]
    public void AllEncodingsRejectTruncatedInputs()
    {
        foreach (IIntegerEncoding encoding in Encodings)
        {
            Span<byte> buffer = stackalloc byte[encoding.MaxEncodedLength];
            ulong value = encoding == QuicEncoding.Instance ? 15_293UL : 16_384UL;
            int length = encoding.Encode(value, buffer);

            Assert.False(encoding.TryDecode(ReadOnlySpan<byte>.Empty, out _, out _));
            if (length > 1)
                Assert.False(encoding.TryDecode(buffer.Slice(0, length - 1), out _, out _), encoding.GetType().Name + " accepted a truncated input.");
        }
    }

    [Fact]
    public void EncodingSpecificInvalidInputsAreRejected()
    {
        Assert.False(Leb128Encoding.Instance.TryDecode(FromHex("80808080808080808002"), out _, out _));
        Assert.False(Leb128Encoding.Instance.TryDecode(FromHex("80808080808080808080"), out _, out _));
        Assert.False(CborEncoding.Instance.TryDecode(FromHex("E0"), out _, out _));
        Assert.False(DlugoszEncoding.Instance.TryDecode(FromHex("FA"), out _, out _));
        Assert.False(DlugoszEncoding.Instance.TryDecode(FromHex("FB"), out _, out _));
        Assert.False(DlugoszEncoding.Instance.TryDecode(FromHex("FF"), out _, out _));
        Assert.False(VlqEncoding.Instance.TryDecode(FromHex("81808080808080808080"), out _, out _));
        Assert.False(VlqEncoding.Instance.TryDecode(FromHex("82808080808080808000"), out _, out _));
        Assert.False(Bijou64Encoding.Instance.TryDecode(FromHex("FFFFFFFFFFFFFFFFFF"), out _, out _));
        Assert.False(Bijou32Encoding.Instance.TryDecode(FromHex("FFFFFFFFFF"), out uint _, out _));
        Assert.False(Varu64Encoding.Instance.TryDecode(FromHex("F800"), out _, out _));
        Assert.False(Varu64Encoding.Instance.TryDecode(FromHex("F82A"), out _, out _));
        Assert.False(Varu64Encoding.Instance.TryDecode(FromHex("F9002A"), out _, out _));
        Assert.False(Varu64Encoding.Instance.TryDecode(FromHex("FF000102030405"), out _, out _));
        Assert.False(Varu64Encoding.Instance.TryDecode(FromHex("FF00010203040506"), out _, out _));
        Assert.False(Vu128Encoding.Instance.TryDecode(FromHex("F8"), out ulong _, out _));
    }

    [Fact]
    public void Base128OverflowFailureClearsOutputs()
    {
        Assert.False(Leb128Encoding.Instance.TryDecode(FromHex("80808080808080808002"), out ulong value, out int bytesRead));
        Assert.Equal(0UL, value);
        Assert.Equal(0, bytesRead);
    }

    [Fact]
    public void QuicRejectsValuesOutsideItsDomain()
    {
        byte[] buffer = new byte[QuicEncoding.Instance.MaxEncodedLength];

        Assert.Throws<ArgumentOutOfRangeException>(() => QuicEncoding.Instance.GetEncodedLength(QuicEncoding.MaxValue + 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => QuicEncoding.Instance.Encode(QuicEncoding.MaxValue + 1, buffer));
    }

    [Fact]
    public void Bijou32RejectsValuesOutsideItsDomain()
    {
        byte[] buffer = new byte[Bijou32Encoding.Instance.MaxEncodedLength];

        Assert.Throws<ArgumentOutOfRangeException>(() => Bijou32Encoding.Instance.GetEncodedLength((ulong)uint.MaxValue + 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Bijou32Encoding.Instance.Encode((ulong)uint.MaxValue + 1, buffer));
    }

    [Fact]
    public void SizeComparisonTableShowsOptimalEncodingForIntegerSets()
    {
        (string Name, ulong[] Values)[] sets =
        [
            ("tiny", [0, 1, 2, 10, 23, 42, 63]),
            ("byte", [64, 127, 128, 239, 240, 247, 248, 255]),
            ("u16", [256, 1_000, 16_383, 16_384, 65_535]),
            ("u32", [65_536, 1_000_000, 268_435_455, uint.MaxValue]),
            ("u64", [4_294_967_296, 72_340_172_838_076_920, QuicEncoding.MaxValue, ulong.MaxValue])
        ];

        string header = "| Set | " + string.Join(" | ", Encodings.Select(x => x.GetType().Name)) + " | Best |";
        string separator = "| --- | " + string.Join(" | ", Encodings.Select(_ => "---:")) + " | --- |";
        output.WriteLine(header);
        output.WriteLine(separator);

        foreach ((string name, ulong[] values) in sets)
        {
            List<string> cells = new List<string>();
            string best = string.Empty;
            double bestPercent = double.MaxValue;

            foreach (IIntegerEncoding encoding in Encodings)
            {
                if (encoding == QuicEncoding.Instance && values.Any(x => x > QuicEncoding.MaxValue))
                {
                    cells.Add("n/a");
                    continue;
                }

                double percent = values.Average(x => encoding.GetEncodedLength(x) / 8.0 * 100.0);
                cells.Add(percent.ToString("0.#", CultureInfo.InvariantCulture) + "%");
                if (percent < bestPercent)
                {
                    bestPercent = percent;
                    best = encoding.GetType().Name;
                }
            }

            output.WriteLine("| " + name + " | " + string.Join(" | ", cells) + " | " + best + " |");
            Assert.False(string.IsNullOrEmpty(best));
            Assert.True(bestPercent <= 100.0);
        }
    }

    private static string ToHex(ReadOnlySpan<byte> bytes)
    {
        char[] chars = new char[bytes.Length * 2];
        const string Hex = "0123456789ABCDEF";

        for (int i = 0; i < bytes.Length; i++)
        {
            chars[i * 2] = Hex[bytes[i] >> 4];
            chars[(i * 2) + 1] = Hex[bytes[i] & 0x0f];
        }

        return new string(chars);
    }

    private static byte[] FromHex(string hex)
    {
        byte[] bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = byte.Parse(hex.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

        return bytes;
    }
}