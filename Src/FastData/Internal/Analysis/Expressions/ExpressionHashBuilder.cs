using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Genbox.FastData.Internal.Misc;
using Genbox.FastData.Misc;
using JetBrains.Annotations;
using static System.Linq.Expressions.Expression;

namespace Genbox.FastData.Internal.Analysis.Expressions;

internal static class ExpressionHashBuilder
{
    internal static Expression<HashFunc<string>> BuildFull(Mixer mixer, Avalanche avalanche)
    {
        ParameterExpression input = Parameter(typeof(string), "value");

        ParameterExpression length = Variable(typeof(int), "length");
        ParameterExpression offset = Variable(typeof(int), "offset");
        ParameterExpression hash = Variable(typeof(ulong), "hash");
        List<Expression> ex = new List<Expression>();

        // int offset = 0
        ex.Add(Assign(offset, Constant(0)));

        // int hash = 352654597U
        ex.Add(Assign(hash, Constant(352654597UL)));

        // int length = input.Length
        ex.Add(Assign(length, Property(input, "Length")));

        // while (length > 0)
        LabelTarget breakLabel = Label();
        LoopExpression loop = Loop(
            IfThenElse(
                GreaterThan(length, Constant(0)),
                Block(
                    Assign(hash, mixer(hash, GetReadFunc(input, offset, 1))),
                    AddAssign(offset, Constant(1)),
                    SubtractAssign(length, Constant(1))),
                Break(breakLabel)
            ),
            breakLabel
        );
        ex.Add(loop);

        // hash = avalanche(hash);
        ex.Add(Assign(hash, avalanche(hash)));

        UnaryExpression block = Convert(Block([length, offset, hash], ex), typeof(ulong));
        return Lambda<HashFunc<string>>(block, input);
    }

    internal static Expression<HashFunc<string>> Build(ArraySegment[] segments, Mixer mixer, Avalanche avalanche)
    {
        ParameterExpression input = Parameter(typeof(string), "value");

        ParameterExpression length = Variable(typeof(int), "length");
        ParameterExpression offset = Variable(typeof(int), "offset");
        ParameterExpression hash = Variable(typeof(ulong), "hash");
        List<Expression> ex = new List<Expression>();

        // int length = input.Length
        ex.Add(Assign(length, Property(input, "Length")));

        if (segments.Length == 1 && segments[0].Length == -1)
            OutputFullHash(ex, segments[0], length, input, hash, offset, mixer);
        else
        {
            foreach (ArraySegment seg in segments)
            {
                // int offset = <offset>
                ex.Add(Assign(offset, Constant((int)seg.Offset)));

                int rem = seg.Length;
                while (rem > 0)
                {
                    int chunk = rem >= 8 ? 8 :
                        rem >= 4 ? 4 :
                        rem >= 2 ? 2 : 1;

                    // Mixer(hash, Read(data, offset))
                    // offset += chunk
                    ex.Add(Assign(hash, mixer(hash, GetReadFunc(input, offset, chunk))));
                    ex.Add(AddAssign(offset, Constant(chunk)));
                    rem -= chunk;
                }
            }
        }

        // hash = avalanche(hash);
        ex.Add(Assign(hash, avalanche(hash)));

        UnaryExpression block = Convert(Block([length, offset, hash], ex), typeof(ulong));
        return Lambda<HashFunc<string>>(block, input);
    }

    private static void OutputFullHash(List<Expression> ex, ArraySegment seg, Expression length, Expression input, Expression hash, Expression offset, Mixer mixer)
    {
        // int offset = <offset>
        // int length -= offset
        ex.Add(Assign(offset, Constant((int)seg.Offset)));
        ex.Add(SubtractAssign(length, offset));

        // while (length > 0)
        LabelTarget breakLabel = Label();
        LoopExpression loop = Loop(
            IfThenElse(
                GreaterThan(length, Constant(0)),
                BuildDynamicChunkBlock(input, offset, length, hash, mixer),
                Break(breakLabel)
            ),
            breakLabel
        );
        ex.Add(loop);
    }

    // Branch for chunk size of 8, 4, 2, 1
    private static ConditionalExpression BuildDynamicChunkBlock(Expression data, Expression offset, Expression length, Expression hash, Mixer mixer) =>
        IfThenElse(
            GreaterThanOrEqual(length, Constant(8)),
            DynamicChunk(8, data, offset, length, hash, mixer),
            IfThenElse(
                GreaterThanOrEqual(length, Constant(4)),
                DynamicChunk(4, data, offset, length, hash, mixer),
                IfThenElse(
                    GreaterThanOrEqual(length, Constant(2)),
                    DynamicChunk(2, data, offset, length, hash, mixer),
                    DynamicChunk(1, data, offset, length, hash, mixer)
                )
            )
        );

    private static BlockExpression DynamicChunk(int chunk, Expression data, Expression offset, Expression length, Expression hash, Mixer mixer)
    {
        UnaryExpression readCall = GetReadFunc(data, offset, chunk);

        // hash = mixer(Read(data, offset))
        // offset += chunk
        // length -= chunk
        return Block(
            Assign(hash, mixer(hash, readCall)),
            AddAssign(offset, Constant(chunk)),
            SubtractAssign(length, Constant(chunk))
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
        return Convert(Call(readFunc, data, idx), typeof(ulong));
    }

    [UsedImplicitly(ImplicitUseTargetFlags.Members)]
    private static class ReaderHelpers
    {
        public static byte ReadU8(string ptr, int offset) => (byte)Unsafe.Add(ref MemoryMarshal.GetReference(ptr.AsSpan()), offset);
        public static ushort ReadU16(string ptr, int offset) => Unsafe.ReadUnaligned<ushort>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref MemoryMarshal.GetReference(ptr.AsSpan()), offset)));
        public static uint ReadU32(string ptr, int offset) => Unsafe.ReadUnaligned<uint>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref MemoryMarshal.GetReference(ptr.AsSpan()), offset)));
        public static ulong ReadU64(string ptr, int offset) => Unsafe.ReadUnaligned<ulong>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref MemoryMarshal.GetReference(ptr.AsSpan()), offset)));
    }
}