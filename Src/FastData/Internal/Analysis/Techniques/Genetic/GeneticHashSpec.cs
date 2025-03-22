using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Internal.Analysis.Misc;

namespace Genbox.FastData.Internal.Analysis.Techniques.Genetic;

[StructLayout(LayoutKind.Auto)]
[SuppressMessage("Security", "CA5394:Do not use insecure randomness")]
[SuppressMessage("Major Code Smell", "S125:Sections of code should not be commented out")]
internal struct GeneticHashSpec(int mixerSeed, int mixerIterations, int avalancheSeed, int avalancheIterations, StringSegment[] segments) : IHashSpec
{
    internal int MixerSeed = mixerSeed;
    internal int MixerIterations = mixerIterations;
    internal int AvalancheSeed = avalancheSeed;
    internal int AvalancheIterations = avalancheIterations;
    internal StringSegment[] Segments = segments;

    public readonly Func<string, uint> GetFunction() => Hash;
    public Func<string, string, bool> GetEqualFunction() => (s, s1) => true;

    public readonly string GetSource()
        => $$"""
                 public static uint Hash(string str)
                 {
                     ref byte ptr = ref Unsafe.As<char, byte>(ref MemoryMarshal.GetReference(str.AsSpan()));
                     int length = str.Length * 2;
                     ulong acc = 42;

                     if (length >= 32)
                     {
                         ref byte limit = ref Unsafe.Add(ref ptr, length - 31);

                         ulong acc0 = 0; //TODO: Seed correctly
                         ulong acc1 = 0;
                         ulong acc2 = 0;
                         ulong acc3 = 0;

                         do
                         {
                             acc0 = Mixer(acc0, Unsafe.ReadUnaligned<ulong>(ref ptr));
                             ptr = ref Unsafe.Add(ref ptr, 8);

                             acc1 = Mixer(acc1, Unsafe.ReadUnaligned<ulong>(ref ptr));
                             ptr = ref Unsafe.Add(ref ptr, 8);

                             acc2 = Mixer(acc2, Unsafe.ReadUnaligned<ulong>(ref ptr));
                             ptr = ref Unsafe.Add(ref ptr, 8);

                             acc3 = Mixer(acc3, Unsafe.ReadUnaligned<ulong>(ref ptr));
                             ptr = ref Unsafe.Add(ref ptr, 8);
                         } while (Unsafe.IsAddressLessThan(ref ptr, ref limit));

                         acc = Mixer(acc, acc0);
                         acc = Mixer(acc, acc1);
                         acc = Mixer(acc, acc2);
                         acc = Mixer(acc, acc3);

                         //TODO: acc += length;
                         length &= 31;
                     }

                     if (length >= 16)
                     {
                         ref byte limit = ref Unsafe.Add(ref ptr, length - 15);

                         ulong acc0 = 0; //TODO: Seed correctly
                         ulong acc1 = 0;

                         do
                         {
                             acc0 = Mixer(acc0, Unsafe.ReadUnaligned<ulong>(ref ptr));
                             ptr = ref Unsafe.Add(ref ptr, 8);

                             acc1 = Mixer(acc1, Unsafe.ReadUnaligned<ulong>(ref ptr));
                             ptr = ref Unsafe.Add(ref ptr, 8);
                         } while (Unsafe.IsAddressLessThan(ref ptr, ref limit));

                         acc = Mixer(acc, acc0);
                         acc = Mixer(acc, acc1);

                         //TODO: acc += length;
                         length &= 15;
                     }

                     while (length >= 8)
                     {
                         acc = Mixer(acc, Unsafe.ReadUnaligned<ulong>(ref ptr));
                         ptr = ref Unsafe.Add(ref ptr, 8);
                         length -= 8;
                     }

                     if (length >= 4)
                     {
                         acc = Mixer(acc, Unsafe.ReadUnaligned<uint>(ref ptr));
                         ptr = ref Unsafe.Add(ref ptr, 4);
                         length -= 4;
                     }

                     while (length > 0)
                     {
                         acc = Mixer(acc, Unsafe.ReadUnaligned<byte>(ref ptr));
                         ptr = ref Unsafe.Add(ref ptr, 1);
                         length--;
                     }

                     var accParam = acc;
                     {{ExpressionConverter.Instance.GetCode(GetAvalanche())}}
                     return (uint)acc;
                 }

                 public static ulong Mixer(ulong accParam, ulong inputParam)
                 {
                     ulong {{ExpressionConverter.Instance.GetCode(GetMixer())}}
                     return acc;
                 }
             """;

    private readonly uint Hash(string str)
    {
        ref byte ptr = ref Unsafe.As<char, byte>(ref MemoryMarshal.GetReference(str.AsSpan()));
        int length = str.Length * 2;
        ulong acc = 42;

        //TODO: move out for perf
        Func<ulong, ulong, ulong> mixer = GetMixer().Compile();
        Func<ulong, ulong> avalanche = GetAvalanche().Compile();

        if (length >= 32)
        {
            ref byte limit = ref Unsafe.Add(ref ptr, length - 31);

            ulong acc0 = 0; //TODO: Seed correctly
            ulong acc1 = 0;
            ulong acc2 = 0;
            ulong acc3 = 0;

            do
            {
                acc0 = mixer(acc0, Unsafe.ReadUnaligned<ulong>(ref ptr));
                ptr = ref Unsafe.Add(ref ptr, 8);

                acc1 = mixer(acc1, Unsafe.ReadUnaligned<ulong>(ref ptr));
                ptr = ref Unsafe.Add(ref ptr, 8);

                acc2 = mixer(acc2, Unsafe.ReadUnaligned<ulong>(ref ptr));
                ptr = ref Unsafe.Add(ref ptr, 8);

                acc3 = mixer(acc3, Unsafe.ReadUnaligned<ulong>(ref ptr));
                ptr = ref Unsafe.Add(ref ptr, 8);
            } while (Unsafe.IsAddressLessThan(ref ptr, ref limit));

            acc = mixer(acc, acc0);
            acc = mixer(acc, acc1);
            acc = mixer(acc, acc2);
            acc = mixer(acc, acc3);

            //TODO: acc += length;
            length &= 31;
        }

        if (length >= 16)
        {
            ref byte limit = ref Unsafe.Add(ref ptr, length - 15);

            ulong acc0 = 0; //TODO: Seed correctly
            ulong acc1 = 0;

            do
            {
                acc0 = mixer(acc0, Unsafe.ReadUnaligned<ulong>(ref ptr));
                ptr = ref Unsafe.Add(ref ptr, 8);

                acc1 = mixer(acc1, Unsafe.ReadUnaligned<ulong>(ref ptr));
                ptr = ref Unsafe.Add(ref ptr, 8);
            } while (Unsafe.IsAddressLessThan(ref ptr, ref limit));

            acc = mixer(acc, acc0);
            acc = mixer(acc, acc1);

            //TODO: acc += length;
            length &= 15;
        }

        while (length >= 8)
        {
            acc = mixer(acc, Unsafe.ReadUnaligned<ulong>(ref ptr));
            ptr = ref Unsafe.Add(ref ptr, 8);
            length -= 8;
        }

        if (length >= 4)
        {
            acc = mixer(acc, Unsafe.ReadUnaligned<uint>(ref ptr));
            ptr = ref Unsafe.Add(ref ptr, 4);
            length -= 4;
        }

        while (length > 0)
        {
            acc = mixer(acc, Unsafe.ReadUnaligned<byte>(ref ptr));
            ptr = ref Unsafe.Add(ref ptr, 1);
            length--;
        }

        return (uint)avalanche(acc);
    }

    internal readonly Expression<Func<ulong, ulong, ulong>> GetMixer()
    {
        ParameterExpression accParam = Expression.Parameter(typeof(ulong), "accParam");
        ParameterExpression inputParam = Expression.Parameter(typeof(ulong), "inputParam");
        ParameterExpression acc = Expression.Parameter(typeof(ulong), "acc");

        IEnumerable<Expression> expressions = CreateMixer(MixerIterations, new Random(MixerSeed), acc);

        BinaryExpression initLocalAcc = Expression.Assign(acc, Expression.Add(accParam, Expression.Multiply(inputParam, Expression.Constant((ulong)Seeds[0]))));
        BlockExpression block = Expression.Block([acc], expressions.Prepend(initLocalAcc));
        return Expression.Lambda<Func<ulong, ulong, ulong>>(block, accParam, inputParam);
    }

    internal readonly Expression<Func<ulong, ulong>> GetAvalanche()
    {
        ParameterExpression accParam = Expression.Parameter(typeof(ulong), "accParam");
        ParameterExpression acc = Expression.Parameter(typeof(ulong), "acc");

        IEnumerable<Expression> expressions = CreateMixer(AvalancheIterations, new Random(AvalancheSeed), acc);

        BinaryExpression initLocalAcc = Expression.Assign(acc, accParam);
        BlockExpression block = Expression.Block([acc], expressions.Prepend(initLocalAcc));
        return Expression.Lambda<Func<ulong, ulong>>(block, accParam);
    }

    private static IEnumerable<Expression> CreateMixer(int iterations, Random rng, ParameterExpression acc)
    {
        for (int i = 0; i < iterations; i++)
        {
            int choice = rng.Next(1, 7);

            Expression body = acc;
            body = choice switch
            {
                1 => Add(body, Seeds[rng.Next(0, Seeds.Length)]),
                2 => Multiply(body, Seeds[rng.Next(0, Seeds.Length)]),
                3 => RotateLeft(body, rng.Next(1, 64)),
                4 => RotateRight(body, rng.Next(1, 64)),
                5 => XorShift(body, rng.Next(1, 64)),
                6 => Square(body),
                _ => throw new InvalidOperationException("Value out of range")
            };

            yield return Expression.Assign(acc, body);
        }
    }

    private static BinaryExpression Add(Expression e, ulong x) => Expression.Add(e, Expression.Constant(x));
    private static BinaryExpression Multiply(Expression e, ulong x) => Expression.Multiply(e, Expression.Constant(x));
    private static BinaryExpression RotateLeft(Expression e, int x) => Expression.Or(Expression.LeftShift(e, Expression.Constant(x)), Expression.RightShift(e, Expression.Constant(64 - x)));
    private static BinaryExpression RotateRight(Expression e, int x) => Expression.Or(Expression.RightShift(e, Expression.Constant(x)), Expression.LeftShift(e, Expression.Constant(64 - x)));
    private static BinaryExpression XorShift(Expression e, int x) => Expression.ExclusiveOr(e, Expression.RightShift(e, Expression.Constant(x)));
    private static BinaryExpression Square(Expression e) => Expression.Add(Expression.Or(Expression.Constant(1UL), e), Expression.Multiply(e, e));

    // A good seed has the following properties:
    // - Odd: Avoids getting the mixer stuck in a loop of 0.
    // - Large: Means we push a lot of the lower bits into higher bits. Gives better avalanche.
    // - Low bias: No correlation between input bits and output bits
    private static readonly uint[] Seeds =
    [
        0x85EBCA6B, 0xC2B2AE35, //Murmur
        0x45D9F3B, // Degski
        0x9E3779B9, // FP32
        0x7FEB352D, 0x846CA68B, // Lowbias
        0xED5AD4BB, 0xAC4C1B51, 0x31848BAB, //Triple
        0x85EBCA77, 0xC2B2AE3D, // XXHash2
    ];

    public sealed class ExpressionConverter : ExpressionVisitor
    {
        private readonly StringBuilder _sb = new StringBuilder();

        private ExpressionConverter() {}
        internal static ExpressionConverter Instance => new ExpressionConverter();

        public string GetCode(Expression expression)
        {
            _sb.Clear();
            Visit(expression);
            return _sb.ToString();
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.Assign)
            {
                Visit(node.Left);
                _sb.Append(" = ");
                Visit(node.Right);
            }
            else
            {
                _sb.Append('(');
                Visit(node.Left);
                _sb.Append(GetBinaryOperator(node.NodeType));
                Visit(node.Right);
                _sb.Append(')');
            }

            return node;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            _sb.Append(node.Value);
            return node;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            _sb.Append(node.Name);
            return node;
        }

        protected override Expression VisitBlock(BlockExpression node)
        {
            foreach (Expression? expression in node.Expressions)
            {
                Visit(expression);
                _sb.AppendLine(";");
            }
            return node;
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            Visit(node.Body);
            return node;
        }

        private static string GetBinaryOperator(ExpressionType type)
        {
            return type switch
            {
                ExpressionType.Add => " + ",
                ExpressionType.Multiply => " * ",
                ExpressionType.ExclusiveOr => " ^ ",
                ExpressionType.LeftShift => " << ",
                ExpressionType.RightShift => " >> ",
                ExpressionType.Or => " | ",
                _ => throw new NotSupportedException($"Operator {type} is not supported.")
            };
        }
    }
}