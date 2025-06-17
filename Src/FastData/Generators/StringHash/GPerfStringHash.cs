using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.StringHash.Framework;
using Genbox.FastData.Internal.Abstracts;
using static System.Linq.Expressions.Expression;

namespace Genbox.FastData.Generators.StringHash;

internal sealed record GPerfStringHash : IStringHash
{
    private Expression<HashFunc<string>>? _expression; //We cache the expression because it does not change

    internal GPerfStringHash(int[] associationValues, int[] alphaIncrements, int[] positions, uint minLen)
    {
        AssociationValues = associationValues;
        AlphaIncrements = alphaIncrements;
        Positions = positions;
        MinLen = minLen;
    }

    internal int[] AssociationValues { get; }
    internal int[] AlphaIncrements { get; }
    internal int[] Positions { get; }
    internal uint MinLen { get; }

    public HashFunc<string> GetHashFunction() => GetExpression().Compile();
    public Expression<HashFunc<string>> GetExpression() => _expression ??= CreateExpression();
    public ReaderFunctions Functions => ReaderFunctions.None;
    public State[] State => [new State(nameof(AssociationValues), typeof(int), AssociationValues)];

    private Expression<HashFunc<string>> CreateExpression()
    {
        ParameterExpression str = Parameter(typeof(string), "value");
        ParameterExpression hash = Variable(typeof(ulong), "hash");

        PropertyInfo assoProp = typeof(GPerfStringHash).GetProperty(nameof(AssociationValues), BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)!;
        Expression asso = Property(Constant(this, typeof(GPerfStringHash)), assoProp);

        List<Expression> ex = new List<Expression>
        {
            // hash = 0UL
            Assign(hash, Constant(0UL)),
        };

        int key = Positions[0];

        //Keys are sorted, so we can do some deductions:
        //- If the key is -1, it means the hash is simply just the last character. In that case, we can render a simple expression
        //- If the key is less than the minimum strength length, we can render a simple expression.

        if (key == -1 || key < MinLen)
        {
            foreach (int pos in Positions)
                ex.Add(Assign(hash, Add(hash, GetPosition(asso, str, pos))));
        }
        else
        {
            // Conditional branches down from key to MinLen
            key++;
            do
            {
                ex.Add(IfThen(
                    GreaterThanOrEqual(Property(str, "Length"), Constant(key)),
                    Assign(hash, Add(hash, Convert(GetPosition(asso, str, key - 1), typeof(ulong)))
                    )));
            } while (key-- > MinLen);

            if (key == -1)
                ex.Add(Assign(hash, Add(hash, Convert(GetPosition(asso, str, key), typeof(ulong)))));
        }

        BlockExpression body = Block([hash], ex);
        return Lambda<HashFunc<string>>(body, str);
    }

    private UnaryExpression GetPosition(Expression asso, Expression str, int pos)
    {
        Expression idx;

        if (pos == -1)
            idx = Subtract(Property(str, "Length"), Constant(1));
        else
        {
            // Cannot use ArrayIndex here because strings are not arrays
            PropertyInfo? charsProp = typeof(string).GetProperty("Chars", [typeof(int)]);

            // Fixed position and optional increment
            idx = MakeIndex(str, charsProp, [Constant(pos)]);

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