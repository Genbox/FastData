using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Genbox.FastData.Specs.Misc;
using JetBrains.Annotations;

namespace Genbox.FastData.Specs.Hash;

public delegate Expression Mixer(Expression hash, Expression readFunc);

public delegate Expression Avalanche(Expression hash);

public static class ExpressionHashBuilder
{
    public static Expression<HashFunc> BuildFull(Mixer mixer, Avalanche avalanche)
    {
        ParameterExpression input = Expression.Parameter(typeof(byte).MakeByRefType(), "input");
        ParameterExpression length = Expression.Parameter(typeof(int), "length");

        ParameterExpression offset = Expression.Variable(typeof(int), "offset");
        ParameterExpression hash = Expression.Variable(typeof(ulong), "hash");
        List<Expression> ex = new List<Expression>();

        // int offset = 0
        ex.Add(Expression.Assign(offset, Expression.Constant(0)));

        // int hash = 352654597U
        ex.Add(Expression.Assign(hash, Expression.Constant(352654597UL)));

        // while (length > 0)
        LabelTarget breakLabel = Expression.Label();
        LoopExpression loop = Expression.Loop(
            Expression.IfThenElse(
                Expression.GreaterThan(length, Expression.Constant(0)),
                Expression.Block(
                    Expression.Assign(hash, mixer(hash, GetReadFunc(input, offset, 1))),
                    Expression.AddAssign(offset, Expression.Constant(1)),
                    Expression.SubtractAssign(length, Expression.Constant(1))),
                Expression.Break(breakLabel)
            ),
            breakLabel
        );
        ex.Add(loop);

        // hash = avalanche(hash);
        ex.Add(Expression.Assign(hash, avalanche(hash)));

        UnaryExpression block = Expression.Convert(Expression.Block([offset, hash], ex), typeof(ulong));
        return Expression.Lambda<HashFunc>(block, input, length);
    }

    public static Expression<HashFunc> Build(StringSegment[] segments, Mixer mixer, Avalanche avalanche)
    {
        ParameterExpression input = Expression.Parameter(typeof(byte).MakeByRefType(), "input");
        ParameterExpression length = Expression.Parameter(typeof(int), "length");

        ParameterExpression offset = Expression.Variable(typeof(int), "offset");
        ParameterExpression hash = Expression.Variable(typeof(ulong), "hash");
        List<Expression> ex = new List<Expression>();

        if (segments.Length == 1 && segments[0].Length == -1)
        {
            OutputFullHash(ex, segments[0], length, input, hash, offset, mixer);
        }
        else
        {
            foreach (StringSegment seg in segments)
            {
                // int offset = <offset>
                ex.Add(Expression.Assign(offset, Expression.Constant((int)seg.Offset)));

                int rem = seg.Length;
                while (rem > 0)
                {
                    int chunk = rem >= 8 ? 8 :
                        rem >= 4 ? 4 :
                        rem >= 2 ? 2 : 1;

                    // Mixer(hash, Read(data, offset))
                    // offset += chunk
                    ex.Add(Expression.Assign(hash, mixer(hash, GetReadFunc(input, offset, chunk))));
                    ex.Add(Expression.AddAssign(offset, Expression.Constant(chunk)));
                    rem -= chunk;
                }
            }
        }

        // hash = avalanche(hash);
        ex.Add(Expression.Assign(hash, avalanche(hash)));

        UnaryExpression block = Expression.Convert(Expression.Block([offset, hash], ex), typeof(ulong));
        return Expression.Lambda<HashFunc>(block, input, length);
    }

    private static void OutputFullHash(List<Expression> ex, StringSegment seg, Expression length, Expression input, Expression hash, Expression offset, Mixer mixer)
    {
        // int offset = <offset>
        // int length -= offset
        ex.Add(Expression.Assign(offset, Expression.Constant((int)seg.Offset)));
        ex.Add(Expression.SubtractAssign(length, offset));

        // while (length > 0)
        LabelTarget breakLabel = Expression.Label();
        LoopExpression loop = Expression.Loop(
            Expression.IfThenElse(
                Expression.GreaterThan(length, Expression.Constant(0)),
                BuildDynamicChunkBlock(input, offset, length, hash, mixer),
                Expression.Break(breakLabel)
            ),
            breakLabel
        );
        ex.Add(loop);
    }

    private static ConditionalExpression BuildDynamicChunkBlock(Expression data, Expression offset, Expression length, Expression hash, Mixer mixer)
    {
        // Branch for chunk size of 8, 4, 2, 1
        return Expression.IfThenElse(
            Expression.GreaterThanOrEqual(length, Expression.Constant(8)),
            DynamicChunk(8, data, offset, length, hash, mixer),
            Expression.IfThenElse(
                Expression.GreaterThanOrEqual(length, Expression.Constant(4)),
                DynamicChunk(4, data, offset, length, hash, mixer),
                Expression.IfThenElse(
                    Expression.GreaterThanOrEqual(length, Expression.Constant(2)),
                    DynamicChunk(2, data, offset, length, hash, mixer),
                    DynamicChunk(1, data, offset, length, hash, mixer)
                )
            )
        );
    }

    private static BlockExpression DynamicChunk(int chunk, Expression data, Expression offset, Expression length, Expression hash, Mixer mixer)
    {
        UnaryExpression readCall = GetReadFunc(data, offset, chunk);

        // hash = mixer(Read(data, offset))
        // offset += chunk
        // length -= chunk
        return Expression.Block(
            Expression.Assign(hash, mixer(hash, readCall)),
            Expression.AddAssign(offset, Expression.Constant(chunk)),
            Expression.SubtractAssign(length, Expression.Constant(chunk))
        );
    }

    private static UnaryExpression GetReadFunc(Expression data, Expression idx, int length)
    {
        string name = length switch
        {
            1 => "ReadU8",
            2 => "ReadU16",
            4 => "ReadU32",
            8 => "ReadU64",
            _ => throw new InvalidOperationException($"Invalid length: {length.ToString(NumberFormatInfo.InvariantInfo)}")
        };

        MethodInfo? readFunc = typeof(ReaderHelpers).GetMethod(name, BindingFlags.Static | BindingFlags.Public);

        if (readFunc == null)
            throw new InvalidOperationException("Could not find method");

        // Read(data, idx)
        return Expression.Convert(Expression.Call(readFunc, data, idx), typeof(ulong));
    }

    [UsedImplicitly(ImplicitUseTargetFlags.Members)]
    private static class ReaderHelpers
    {
        public static byte ReadU8(ref byte ptr, int offset) => Unsafe.Add(ref ptr, offset);
        public static ushort ReadU16(ref byte ptr, int offset) => Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref ptr, offset));
        public static uint ReadU32(ref byte ptr, int offset) => Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref ptr, offset));
        public static ulong ReadU64(ref byte ptr, int offset) => Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref ptr, offset));
    }
}