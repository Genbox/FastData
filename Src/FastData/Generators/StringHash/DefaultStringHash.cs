using System.Linq.Expressions;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators.StringHash.Framework;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Expressions;
using static System.Linq.Expressions.Expression;

namespace Genbox.FastData.Generators.StringHash;

/// <summary>Hashes the entire string using DJB2 hash</summary>
internal sealed record DefaultStringHash : IStringHash
{
    private readonly GeneratorEncoding _encoding;

    private DefaultStringHash(GeneratorEncoding encoding) => _encoding = encoding;

    internal static DefaultStringHash UTF8Instance { get; } = new DefaultStringHash(GeneratorEncoding.UTF8);
    internal static DefaultStringHash UTF16Instance { get; } = new DefaultStringHash(GeneratorEncoding.UTF16);

    internal static DefaultStringHash GetInstance(GeneratorEncoding enc) => enc == GeneratorEncoding.UTF8 ? UTF8Instance : UTF16Instance;

    public State[]? State => null;
    public Expression<StringHashFunc> GetExpression() => ExpressionHashBuilder.BuildFull(Mixer, Avalanche, _encoding);
    public ReaderFunctions Functions => ReaderFunctions.ReadU16;

    // (((hash << 5) | (hash >> 27)) + hash) ^ Read(data, offset)
    private static Expression Mixer(Expression hash, Expression read) =>
        Assign(hash, ExclusiveOr(Add(Or(LeftShift(hash, Constant(5)), RightShift(hash, Constant(27))), hash), read));

    // 352654597 + (hash * 1566083941)
    private static Expression Avalanche(Expression hash) =>
        Add(Constant(352654597ul), Multiply(hash, Constant(1566083941ul)));
}