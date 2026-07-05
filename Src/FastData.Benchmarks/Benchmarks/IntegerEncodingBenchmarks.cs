using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;
using Genbox.FastData.Benchmarks.Code;
using Genbox.FastData.Internal.Encodings;

namespace Genbox.FastData.Benchmarks.Benchmarks;

[MinIterationCount(35)] // Higher iteration counts than the default because we are measuring small amounts of instructions
[MaxIterationCount(50)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByMethod)]
[Orderer(SummaryOrderPolicy.Method)]
public class IntegerEncodingBenchmarks
{
    private readonly byte[] _encoded = new byte[10 * 4096];
    private readonly int[] _offsets = new int[4096];
    private readonly int[] _lengths = new int[4096];
    private IIntegerEncoding _encoding = null!;
    private ulong[] _values = null!;

    [Params(nameof(Leb128Encoding), nameof(VlqEncoding), nameof(QuicEncoding), nameof(DlugoszEncoding), nameof(SqliteEncoding), nameof(CborEncoding), nameof(Bijou32Encoding), nameof(Bijou64Encoding), nameof(Varu64Encoding), nameof(Vu128Encoding), nameof(PrefixEncoding), nameof(GitPackEncoding))]
    public string Encoding { get; set; } = null!;

    [Params("Tiny", "U16", "U32", "U64")]
    public string Distribution { get; set; } = null!;

    [GlobalSetup]
    public void Setup()
    {
        _encoding = CreateEncoding(Encoding);
        _values = CreateValues(Distribution, _encoding);

        int offset = 0;
        for (int i = 0; i < _values.Length; i++)
        {
            _offsets[i] = offset;
            int length = _encoding.Encode(_values[i], _encoded.AsSpan(offset, _encoding.MaxEncodedLength));
            _lengths[i] = length;
            offset += length;
        }
    }

    [Benchmark]
    public int Encode()
    {
        Span<byte> buffer = stackalloc byte[10];
        int total = 0;
        foreach (ulong value in _values)
            total += _encoding.Encode(value, buffer);

        return total;
    }

    [Benchmark]
    public ulong Decode()
    {
        ulong total = 0;
        for (int i = 0; i < _values.Length; i++)
        {
            ReadOnlySpan<byte> source = _encoded.AsSpan(_offsets[i], _lengths[i]);
            if (!_encoding.TryDecode(source, out ulong value, out _))
                throw new InvalidOperationException("Failed to decode benchmark input.");

            total += value;
        }

        return total;
    }

    private static IIntegerEncoding CreateEncoding(string type) => type switch
    {
        nameof(Leb128Encoding) => Leb128Encoding.Instance,
        nameof(VlqEncoding) => VlqEncoding.Instance,
        nameof(QuicEncoding) => QuicEncoding.Instance,
        nameof(DlugoszEncoding) => DlugoszEncoding.Instance,
        nameof(SqliteEncoding) => SqliteEncoding.Instance,
        nameof(CborEncoding) => CborEncoding.Instance,
        nameof(Bijou32Encoding) => Bijou32Encoding.Instance,
        nameof(Bijou64Encoding) => Bijou64Encoding.Instance,
        nameof(Varu64Encoding) => Varu64Encoding.Instance,
        nameof(Vu128Encoding) => Vu128Encoding.Instance,
        nameof(PrefixEncoding) => PrefixEncoding.Instance,
        nameof(GitPackEncoding) => GitPackEncoding.Instance,
        _ => throw new InvalidOperationException("Unknown encoding: " + type)
    };

    private static ulong[] CreateValues(string distribution, IIntegerEncoding encoding)
    {
        Random rng = new Random(42);
        ulong[] values = new ulong[4096];

        for (int i = 0; i < values.Length; i++)
        {
            values[i] = distribution switch
            {
                "Tiny" => (ulong)rng.Next(0, 128),
                "U16" => (ulong)rng.Next(0, 65_536),
                "U32" => RandomHelper.NextUInt32(rng),
                "U64" => RandomHelper.NextUInt64(rng),
                _ => throw new InvalidOperationException("Unknown distribution: " + distribution)
            };

            if (encoding == QuicEncoding.Instance)
                values[i] &= QuicEncoding.MaxValue;

            if (encoding == Bijou32Encoding.Instance || encoding == Bijou32Encoding.Instance)
                values[i] &= uint.MaxValue;
        }

        return values;
    }
}