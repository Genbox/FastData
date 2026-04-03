using System.Linq.Expressions;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators.StringHash.Framework;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Expressions;

namespace Genbox.FastData.Generators.StringHash;

/// <summary>Hashes the entire string using DJB2 hash</summary>
internal sealed record DefaultStringHash : IStringHash
{
    private readonly GeneratorEncoding _encoding;

    private DefaultStringHash(GeneratorEncoding encoding)
    {
        _encoding = encoding;
    }

    internal static DefaultStringHash ASCIIInstance { get; } = new DefaultStringHash(GeneratorEncoding.AsciiBytes);
    internal static DefaultStringHash UTF8Instance { get; } = new DefaultStringHash(GeneratorEncoding.Utf8Bytes);
    internal static DefaultStringHash UTF16Instance { get; } = new DefaultStringHash(GeneratorEncoding.Utf16CodeUnits);

    public AdditionalData[]? AdditionalData => null;
    public Expression<StringHashFunc> GetExpression() => ExpressionHashBuilder.BuildFull(Mixer, Avalanche, _encoding);

    internal static DefaultStringHash GetInstance(GeneratorEncoding enc) => enc switch
    {
        GeneratorEncoding.AsciiBytes => ASCIIInstance,
        GeneratorEncoding.Utf8Bytes => UTF8Instance,
        GeneratorEncoding.Utf16CodeUnits => UTF16Instance,
        _ => throw new InvalidOperationException($"Unsupported length semantics: {enc}")
    };

    // (((hash << 5) | (hash >> 27)) + hash) ^ Read(data, offset)
    private static Expression Mixer(Expression hash, Expression read) =>
        Assign(hash, ExclusiveOr(Add(Or(LeftShift(hash, Constant(5)), RightShift(hash, Constant(27))), hash), read));

    // 352654597 + (hash * 1566083941)
    private static Expression Avalanche(Expression hash) =>
        Add(Constant(352654597ul), Multiply(hash, Constant(1566083941ul)));
}