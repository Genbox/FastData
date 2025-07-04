using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.StringHash.Framework;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Expressions;
using static System.Linq.Expressions.Expression;

namespace Genbox.FastData.Generators.StringHash;

/// <summary>Hashes the entire string using DJB2 hash</summary>
internal sealed record DefaultStringHash : IStringHash
{
    private Expression<HashFunc<string>>? _expression; //We cache the expression because it does not change

    internal DefaultStringHash() { }

    public State[]? State => null;
    public HashFunc<string> GetHashFunction() => GetExpression().Compile();
    public Expression<HashFunc<string>> GetExpression() => _expression ??= ExpressionHashBuilder.BuildFull(Mixer, Avalanche);
    public ReaderFunctions Functions => ReaderFunctions.ReadU8;

    // (((hash << 5) | (hash >> 27)) + hash) ^ Read(data, offset)
    private static Expression Mixer(Expression hash, Expression read) =>
        Assign(hash, ExclusiveOr(Add(Or(LeftShift(hash, Constant(5)), RightShift(hash, Constant(27))), hash), read));

    // 352654597 + (hash * 1566083941)
    private static Expression Avalanche(Expression hash) =>
        Add(Constant(352654597ul), Multiply(hash, Constant(1566083941ul)));
}