using System.Linq.Expressions;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.StringHash.Framework;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Expressions;

namespace Genbox.FastData.Generators.StringHash;

/// <summary>Hashes the encoded string using xxHash64.</summary>
internal sealed record XxHash64StringHash : IStringHash
{
    private const ulong Prime1 = 0x9E3779B185EBCA87UL;
    private const ulong Prime2 = 0xC2B2AE3D27D4EB4FUL;
    private const ulong Prime3 = 0x165667B19E3779F9UL;
    private const ulong Prime4 = 0x85EBCA77C2B2AE63UL;
    private const ulong Prime5 = 0x27D4EB2F165667C5UL;

    private readonly bool _ignoreCase;

    private XxHash64StringHash(bool ignoreCase = false)
    {
        _ignoreCase = ignoreCase;
    }

    internal static XxHash64StringHash Instance { get; } = new XxHash64StringHash();
    public AdditionalData[]? AdditionalData => null;
    public IEnumerable<IEarlyExit> GetMandatoryExits() => [];

    public Expression<StringHashFunc> GetExpression()
    {
        ParameterExpression input = Parameter(typeof(byte[]), "data");
        ParameterExpression length = Parameter(typeof(int), "length");
        ParameterExpression offset = Variable(typeof(int), "offset");
        ParameterExpression remaining = Variable(typeof(int), "remaining");
        ParameterExpression hash = Variable(typeof(ulong), "hash");
        ParameterExpression round = Variable(typeof(ulong), "round");
        ParameterExpression v1 = Variable(typeof(ulong), "v1");
        ParameterExpression v2 = Variable(typeof(ulong), "v2");
        ParameterExpression v3 = Variable(typeof(ulong), "v3");
        ParameterExpression v4 = Variable(typeof(ulong), "v4");

        Expression[] largeInputExpressions =
        [
            Assign(v1, Constant(unchecked(Prime1 + Prime2))),
            Assign(v2, Constant(Prime2)),
            Assign(v3, Constant(0UL)),
            Assign(v4, Constant(unchecked(0UL - Prime1))),
            BuildStripeLoop(input, offset, remaining, v1, v2, v3, v4),
            Assign(hash, Add(Add(RotateLeft(v1, 1), RotateLeft(v2, 7)), Add(RotateLeft(v3, 12), RotateLeft(v4, 18)))),
            .. BuildMergeRound(hash, v1, round),
            .. BuildMergeRound(hash, v2, round),
            .. BuildMergeRound(hash, v3, round),
            .. BuildMergeRound(hash, v4, round)
        ];
        BlockExpression largeInput = Block(largeInputExpressions);

        List<Expression> expressions =
        [
            Assign(offset, Constant(0)),
            Assign(remaining, length),
            IfThenElse(GreaterThanOrEqual(remaining, Constant(32)), largeInput, Assign(hash, Constant(Prime5))),
            AddAssign(hash, Convert(length, typeof(ulong))),
            BuildEightByteLoop(input, offset, remaining, hash, round),
            IfThen(GreaterThanOrEqual(remaining, Constant(4)), Block(
                Assign(hash, ExclusiveOr(hash, Multiply(ExpressionHashBuilder.GetReadFunc(input, offset, 4, _ignoreCase), Constant(Prime1)))),
                Assign(hash, Add(Multiply(RotateLeft(hash, 23), Constant(Prime2)), Constant(Prime3))),
                AddAssign(offset, Constant(4)),
                SubtractAssign(remaining, Constant(4))
            )),
            BuildByteLoop(input, offset, remaining, hash),
            Assign(hash, ExclusiveOr(hash, RightShift(hash, Constant(33)))),
            Assign(hash, Multiply(hash, Constant(Prime2))),
            Assign(hash, ExclusiveOr(hash, RightShift(hash, Constant(29)))),
            Assign(hash, Multiply(hash, Constant(Prime3))),
            Assign(hash, ExclusiveOr(hash, RightShift(hash, Constant(32))))
        ];

        BlockExpression body = Block([offset, remaining, hash, round, v1, v2, v3, v4], expressions);
        return Lambda<StringHashFunc>(body, input, length);
    }

    internal static XxHash64StringHash GetInstance(GeneratorEncoding encoding, bool ignoreCase = false)
    {
        if (encoding is not (GeneratorEncoding.AsciiBytes or GeneratorEncoding.Utf8Bytes or GeneratorEncoding.Utf16CodeUnits))
            throw new InvalidOperationException($"Unsupported length semantics: {encoding}");

        return ignoreCase ? new XxHash64StringHash(true) : Instance;
    }

    private LoopExpression BuildStripeLoop(Expression input, Expression offset, Expression remaining, Expression v1, Expression v2, Expression v3, Expression v4)
    {
        LabelTarget breakLabel = Label();
        Expression[] stripeExpressions =
        [
            .. BuildRound(v1, ExpressionHashBuilder.GetReadFunc(input, offset, 8, _ignoreCase), v1),
            .. BuildRound(v2, ExpressionHashBuilder.GetReadFunc(input, Add(offset, Constant(8)), 8, _ignoreCase), v2),
            .. BuildRound(v3, ExpressionHashBuilder.GetReadFunc(input, Add(offset, Constant(16)), 8, _ignoreCase), v3),
            .. BuildRound(v4, ExpressionHashBuilder.GetReadFunc(input, Add(offset, Constant(24)), 8, _ignoreCase), v4),
            AddAssign(offset, Constant(32)),
            SubtractAssign(remaining, Constant(32))
        ];

        return Loop(
            IfThenElse(
                GreaterThanOrEqual(remaining, Constant(32)),
                Block(stripeExpressions),
                Break(breakLabel)
            ),
            breakLabel
        );
    }

    private LoopExpression BuildEightByteLoop(Expression input, Expression offset, Expression remaining, Expression hash, Expression round)
    {
        LabelTarget breakLabel = Label();
        Expression[] roundExpressions =
        [
            .. BuildRound(Constant(0UL), ExpressionHashBuilder.GetReadFunc(input, offset, 8, _ignoreCase), round),
            Assign(hash, ExclusiveOr(hash, round)),
            Assign(hash, Add(Multiply(RotateLeft(hash, 27), Constant(Prime1)), Constant(Prime4))),
            AddAssign(offset, Constant(8)),
            SubtractAssign(remaining, Constant(8))
        ];

        return Loop(
            IfThenElse(
                GreaterThanOrEqual(remaining, Constant(8)),
                Block(roundExpressions),
                Break(breakLabel)
            ),
            breakLabel
        );
    }

    private LoopExpression BuildByteLoop(Expression input, Expression offset, Expression remaining, Expression hash)
    {
        LabelTarget breakLabel = Label();
        return Loop(
            IfThenElse(
                GreaterThan(remaining, Constant(0)),
                Block(
                    Assign(hash, ExclusiveOr(hash, Multiply(ExpressionHashBuilder.GetReadFunc(input, offset, 1, _ignoreCase), Constant(Prime5)))),
                    Assign(hash, Multiply(RotateLeft(hash, 11), Constant(Prime1))),
                    AddAssign(offset, Constant(1)),
                    SubtractAssign(remaining, Constant(1))
                ),
                Break(breakLabel)
            ),
            breakLabel
        );
    }

    private static Expression[] BuildRound(Expression accumulator, Expression input, Expression result) =>
    [
        Assign(result, Add(accumulator, Multiply(input, Constant(Prime2)))),
        Assign(result, RotateLeft(result, 31)),
        Assign(result, Multiply(result, Constant(Prime1)))
    ];

    private static Expression[] BuildMergeRound(Expression hash, Expression value, Expression round) =>
    [
        .. BuildRound(Constant(0UL), value, round),
        Assign(hash, ExclusiveOr(hash, round)),
        Assign(hash, Add(Multiply(hash, Constant(Prime1)), Constant(Prime4)))
    ];

    private static BinaryExpression RotateLeft(Expression value, int count) => Or(LeftShift(value, Constant(count)), RightShift(value, Constant(64 - count)));
}