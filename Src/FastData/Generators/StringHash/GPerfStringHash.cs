using System.Linq.Expressions;
using System.Reflection;
using Genbox.FastData.Generators.StringHash.Framework;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Generators.StringHash;

internal sealed record GPerfStringHash : IStringHash
{
    internal GPerfStringHash(int[] associationValues, int[] alphaIncrements, int[] positions, int minLen)
    {
        AssociationValues = associationValues;
        AlphaIncrements = alphaIncrements;
        Positions = positions;
        MinLen = minLen;
    }

    internal int[] AssociationValues { get; }
    internal int[] AlphaIncrements { get; }
    internal int[] Positions { get; }
    internal int MinLen { get; }

    public AdditionalData[] AdditionalData => [new AdditionalData(nameof(AssociationValues), typeof(int), AssociationValues)];

    public Expression<StringHashFunc> GetExpression()
    {
        ParameterExpression value = Parameter(typeof(byte[]), "value");
        ParameterExpression length = Parameter(typeof(int), "length");
        ParameterExpression hash = Variable(typeof(ulong), "hash");

        PropertyInfo assoProp = typeof(GPerfStringHash).GetProperty(nameof(AssociationValues), BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)!;
        Expression asso = Property(Constant(this, typeof(GPerfStringHash)), assoProp);

        List<Expression> ex = new List<Expression>
        {
            // hash = 0UL
            Assign(hash, Constant(0UL))
        };

        // Positions are selected by the analyzer and sorted in gperf order. A selected position contributes only when it exists
        // for the current logical length; positions outside the logical length are skipped.
        foreach (int pos in Positions)
        {
            Expression add = Assign(hash, Add(hash, GetPosition(asso, value, length, pos)));

            // Mirror gperf's fast path: the caller only hashes strings with length >= MinLen, so the last character and any
            // fixed position below MinLen are always valid and do not need a branch.
            if (pos == -1 || pos < MinLen)
                ex.Add(add);
            else
                ex.Add(IfThen(GreaterThanOrEqual(length, Constant(pos + 1)), add));
        }

        // Keep the block's result type as ulong even when the last emitted statement is a guarded IfThen.
        ex.Add(hash);

        BlockExpression body = Block([hash], ex);
        return Lambda<StringHashFunc>(body, value, length);
    }

    private UnaryExpression GetPosition(Expression asso, Expression value, Expression length, int pos)
    {
        Expression idx;

        if (pos == -1)
            idx = Subtract(length, Constant(1));
        else
        {
            // Fixed position and optional increment
            idx = ArrayIndex(value, Constant(pos));

            int inc = AlphaIncrements[pos];

            if (inc != 0)
                idx = Add(Convert(idx, typeof(int)), Constant(inc));
        }

        return Convert(ArrayIndex(asso, Convert(idx, typeof(int))), typeof(ulong));
    }

    public override string ToString() =>
        $"""
         Asso = {string.Join(", ", AssociationValues.Where(x => x != AssociationValues[0]))}
         Alpha = {string.Join(", ", AlphaIncrements)}
         {nameof(Positions)} = {string.Join(", ", Positions)}
         {nameof(MinLen)} = {MinLen}
         """;
}