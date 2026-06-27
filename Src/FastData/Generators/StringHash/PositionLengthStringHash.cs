using System.Linq.Expressions;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits.Exits;
using Genbox.FastData.Generators.StringHash.Framework;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Expressions;
using Genbox.FastData.Internal.Helpers;

namespace Genbox.FastData.Generators.StringHash;

internal sealed record PositionLengthStringHash : IStringHash
{
    private readonly bool _ignoreCase;
    private readonly int _minUnitLength;
    private readonly int _readSize;

    internal PositionLengthStringHash(int[] positions, bool includeLength, int minUnitLength, int readSize, bool ignoreCase)
    {
        Positions = positions;
        IncludeLength = includeLength;
        _minUnitLength = minUnitLength;
        _readSize = readSize;
        _ignoreCase = ignoreCase;
    }

    internal int[] Positions { get; }
    internal bool IncludeLength { get; }
    public AdditionalData[]? AdditionalData => null;

    public IEnumerable<IEarlyExit> GetMandatoryExits() => _minUnitLength > 0 ? [new LengthLessThanEarlyExit(_minUnitLength)] : [];

    public Expression<StringHashFunc> GetExpression()
    {
        ParameterExpression value = Parameter(typeof(byte[]), "data");
        ParameterExpression length = Parameter(typeof(int), "length");
        ParameterExpression hash = Variable(typeof(ulong), "hash");

        List<Expression> ex =
        [
            Assign(hash, IncludeLength ? Convert(length, typeof(ulong)) : Constant(0UL))
        ];

        foreach (int pos in Positions)
        {
            Expression add = AddAssign(hash, ReadPosition(value, length, pos));
            if (pos == -1 || pos + _readSize <= _minUnitLength)
                ex.Add(add);
            else
                ex.Add(IfThen(GreaterThanOrEqual(length, Constant(pos + _readSize)), add));
        }

        ex.Add(hash);

        BlockExpression body = Block([hash], ex);
        return Lambda<StringHashFunc>(body, value, length);
    }

    internal static PositionLengthStringHash CreateFirstLastLength(GeneratorEncoding encoding, bool ignoreCase = false)
    {
        int unitSize = StringHelper.GetSize(encoding);
        return new PositionLengthStringHash([0, -1], true, 1, unitSize, ignoreCase);
    }

    private UnaryExpression ReadPosition(Expression value, Expression length, int pos)
    {
        Expression offset = pos == -1 ? Subtract(length, Constant(_readSize)) : Constant(pos);
        Expression read = ExpressionHashBuilder.GetReadFunc(value, offset, _readSize, _ignoreCase);
        return Convert(read, typeof(ulong));
    }

    public override string ToString() => $"{nameof(Positions)} = {string.Join(", ", Positions)}\n{nameof(IncludeLength)} = {IncludeLength}\n{nameof(_ignoreCase)} = {_ignoreCase}\n";
}