using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Misc;
using static System.Linq.Expressions.Expression;

namespace Genbox.FastData.StringHash;

public sealed record GPerfStringHash : IStringHash
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

    private Expression<HashFunc<string>> CreateExpression()
    {
        ParameterExpression str = Parameter(typeof(string), "str");
        ParameterExpression bytes = Variable(typeof(byte[]), "bytes");
        ParameterExpression hash = Variable(typeof(ulong), "hash");
        ConstantExpression asso = Constant(AssociationValues, typeof(int[]));

        MemberExpression utf8Prop = Property(null, typeof(Encoding).GetProperty(nameof(Encoding.ASCII), BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)!);
        MethodInfo getBytesMeth = typeof(Encoding).GetMethod(nameof(Encoding.GetBytes), [typeof(string)])!;

        List<Expression> ex = new List<Expression>
        {
            // bytes = Encoding.ASCII.GetBytes(str)
            Assign(bytes, Call(utf8Prop, getBytesMeth, str)),

            // hash = 0UL
            Assign(hash, Constant(0UL))
        };

        int key = Positions[0];

        //Keys are sorted, so we can do some deductions:
        //- If the key is -1, it means the hash is simply just the last character. In that case, we can render a simple expression
        //- If the key is less than the minimum strength length, we can render a simple expression.

        if (key == -1 || key < MinLen)
        {
            // simple sum over all positions
            foreach (int pos in Positions)
            {
                ex.Add(Assign(hash, Add(hash, GetPosition(asso, bytes, pos))));
            }
        }
        else
        {
            // conditional branches down from key to MinLen
            key++;
            do
            {
                ex.Add(IfThen(
                    GreaterThanOrEqual(Property(bytes, "Length"), Constant(key)),
                    Assign(hash, Add(hash, GetPosition(asso, bytes, key - 1)))
                ));
            } while (key-- > MinLen);

            if (key == -1)
                ex.Add(Assign(hash, Add(hash, GetPosition(asso, bytes, key))));
        }

        // final return of hash
        ex.Add(hash);

        // wrap locals and expressions in a block, then λ(str) => block
        BlockExpression body = Block([bytes, hash], ex);
        return Lambda<HashFunc<string>>(body, str);
    }

    private UnaryExpression GetPosition(Expression asso, Expression bytes, int pos)
    {
        // compute index into bytes[]
        Expression idx;
        if (pos == -1)
        {
            // last character
            idx = Subtract(Property(bytes, "Length"), Constant(1));
        }
        else
        {
            // fixed position + optional increment
            idx = ArrayIndex(bytes, Constant(pos));

            int inc = AlphaIncrements[pos];

            if (inc != 0)
                idx = Add(Convert(idx, typeof(int)), Constant(inc));
        }

        // asso[(int)idx] cast to ulong for accumulation
        return Convert(ArrayIndex(asso, Convert(idx, typeof(int))), typeof(ulong));
    }

    public override string ToString() =>
        $"""
         Asso = {string.Join(", ", AssociationValues)}
         Alpha = {string.Join(", ", AlphaIncrements)}
         {nameof(Positions)} = {string.Join(", ", Positions)}
         {nameof(MinLen)} = {MinLen}
         """;
}