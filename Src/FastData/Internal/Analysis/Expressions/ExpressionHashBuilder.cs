using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.StringHash.Framework;
using Genbox.FastData.Internal.Enums;
using Genbox.FastData.Internal.Helpers;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Internal.Analysis.Expressions;

internal static class ExpressionHashBuilder
{
    /// <summary>Produces a hash expression that reads the whole input one encoding unit at a time.</summary>
    /// <remarks>This intentionally emits a simple serial loop. Use it for default/debug scenarios where preserving the byte/code-unit order is more important than throughput.</remarks>
    internal static Expression<StringHashFunc> BuildOneByOne(ulong initialSeed, Mixer mixer, Avalanche avalanche, GeneratorEncoding encoding, bool ignoreCase = false)
    {
        ParameterExpression input = Parameter(typeof(byte[]), "data");
        ParameterExpression length = Parameter(typeof(int), "length");
        ParameterExpression offset = Variable(typeof(int), "offset");
        ParameterExpression hash = Variable(typeof(ulong), "hash");
        List<Expression> ex =
        [
            // int offset = 0
            Assign(offset, Constant(0)),

            // int hash = <seed>
            Assign(hash, Constant(initialSeed))
        ];

        int size = StringHelper.GetSize(encoding);

        // while (length > 0)
        LabelTarget breakLabel = Label();
        LoopExpression loop = Loop(
            IfThenElse(
                GreaterThan(length, Constant(0)),
                Block(
                    Assign(hash, mixer(hash, GetReadFunc(input, offset, size, ignoreCase))),
                    AddAssign(offset, Constant(size)),
                    SubtractAssign(length, Constant(size))),
                Break(breakLabel)
            ),
            breakLabel
        );
        ex.Add(loop);

        // hash = avalanche(hash);
        ex.Add(Assign(hash, avalanche(hash)));

        BlockExpression block = Block([offset, hash], ex);
        return Lambda<StringHashFunc>(block, input, length);
    }

    /// <summary>Produces a hash expression for one or more string byte segments.</summary>
    /// <remarks>
    /// Fixed-size segments are emitted as straight-line chunked reads. A single full-tail segment, represented by <see cref="ArraySegment.Length"/> equal to -1,
    /// emits a throughput-oriented loop: four independent 8-byte mixer lanes for 32-byte blocks, then 8/4/2/1-byte tail handling, followed by the avalanche.
    /// </remarks>
    internal static Expression<StringHashFunc> Build(ArraySegment[] segments, Mixer mixer, Avalanche avalanche, bool ignoreCase = false, ulong initialSeed = 0UL)
    {
        ParameterExpression input = Parameter(typeof(byte[]), "data");
        ParameterExpression length = Parameter(typeof(int), "length");
        ParameterExpression offset = Variable(typeof(int), "offset");
        ParameterExpression hash = Variable(typeof(ulong), "hash");
        List<ParameterExpression> variables = [hash];
        List<Expression> ex =
        [
            // int hash = <seed>
            Assign(hash, Constant(initialSeed))
        ];

        if (segments.Length == 1 && segments[0].Length == -1)
        {
            variables.Insert(0, offset);
            BuildFullHash(ex, segments[0], length, input, hash, offset, mixer, ignoreCase, initialSeed);
        }
        else
        {
            foreach (ArraySegment seg in segments)
            {
                Expression offsetExpr = seg.Alignment == Alignment.Right ? Subtract(length, Constant((int)seg.Offset + seg.Length)) : Constant((int)seg.Offset);

                int rem = seg.Length;
                int consumed = 0;
                while (rem > 0)
                {
                    int chunk = rem >= 8 ? 8 :
                        rem >= 4 ? 4 :
                        rem >= 2 ? 2 : 1;
                    Expression readOffset = consumed == 0 ? offsetExpr : Add(offsetExpr, Constant(consumed));

                    // Mixer(hash, Read(data, offset))
                    ex.Add(Assign(hash, mixer(hash, GetReadFunc(input, readOffset, chunk, ignoreCase))));
                    consumed += chunk;
                    rem -= chunk;
                }
            }
        }

        // hash = avalanche(hash);
        ex.Add(Assign(hash, avalanche(hash)));

        BlockExpression block = Block(variables, ex);
        return Lambda<StringHashFunc>(block, input, length);
    }

    /// <summary>Adds the full-tail hash body for an unconstrained segment.</summary>
    /// <remarks>The generated expression mutates <paramref name="length"/> into the number of unread bytes and <paramref name="offset"/> into the current read position.</remarks>
    private static void BuildFullHash(List<Expression> ex, ArraySegment seg, Expression length, Expression input, Expression hash, Expression offset, Mixer mixer, bool ignoreCase, ulong initialSeed)
    {
        ParameterExpression v1 = Variable(typeof(ulong), "v1");
        ParameterExpression v2 = Variable(typeof(ulong), "v2");
        ParameterExpression v3 = Variable(typeof(ulong), "v3");
        ParameterExpression v4 = Variable(typeof(ulong), "v4");

        // int offset = <offset>
        // int length -= offset
        ex.Add(Assign(offset, Constant((int)seg.Offset)));
        ex.Add(SubtractAssign(length, offset));

        // Use independent lanes for large inputs so the generated code exposes instruction-level parallelism.
        ex.Add(IfThen(
            GreaterThanOrEqual(length, Constant(32)),
            Block(
                [v1, v2, v3, v4],
                Assign(v1, Constant(initialSeed)),
                Assign(v2, Constant(initialSeed)),
                Assign(v3, Constant(initialSeed)),
                Assign(v4, Constant(initialSeed)),
                BuildParallelLaneLoop(input, offset, length, mixer, ignoreCase, v1, v2, v3, v4),
                Assign(hash, mixer(hash, v1)),
                Assign(hash, mixer(hash, v2)),
                Assign(hash, mixer(hash, v3)),
                Assign(hash, mixer(hash, v4))
            )));

        ex.Add(BuildChunkLoop(8, input, offset, length, hash, mixer, ignoreCase));
        ex.Add(IfThen(GreaterThanOrEqual(length, Constant(4)), DynamicChunk(4, input, offset, length, hash, mixer, ignoreCase)));
        ex.Add(IfThen(GreaterThanOrEqual(length, Constant(2)), DynamicChunk(2, input, offset, length, hash, mixer, ignoreCase)));
        ex.Add(IfThen(GreaterThan(length, Constant(0)), DynamicChunk(1, input, offset, length, hash, mixer, ignoreCase)));
    }

    /// <summary>Builds the 32-byte loop that feeds four independent 8-byte reads into four mixer lanes.</summary>
    private static LoopExpression BuildParallelLaneLoop(Expression data, Expression offset, Expression length, Mixer mixer, bool ignoreCase, Expression v1, Expression v2, Expression v3, Expression v4)
    {
        LabelTarget breakLabel = Label();
        return Loop(
            IfThenElse(
                GreaterThanOrEqual(length, Constant(32)),
                Block(
                    DynamicChunk(8, data, offset, length, v1, mixer, ignoreCase, false),
                    DynamicChunk(8, data, offset, length, v2, mixer, ignoreCase, false),
                    DynamicChunk(8, data, offset, length, v3, mixer, ignoreCase, false),
                    DynamicChunk(8, data, offset, length, v4, mixer, ignoreCase, false),
                    SubtractAssign(length, Constant(32))),
                Break(breakLabel)
            ),
            breakLabel
        );
    }

    /// <summary>Builds a loop that consumes all remaining complete chunks of the specified size.</summary>
    private static LoopExpression BuildChunkLoop(int chunk, Expression data, Expression offset, Expression length, Expression hash, Mixer mixer, bool ignoreCase)
    {
        LabelTarget breakLabel = Label();
        return Loop(
            IfThenElse(
                GreaterThanOrEqual(length, Constant(chunk)),
                DynamicChunk(chunk, data, offset, length, hash, mixer, ignoreCase),
                Break(breakLabel)
            ),
            breakLabel
        );
    }

    /// <summary>Builds one read/mix step for a 1, 2, 4, or 8 byte chunk.</summary>
    private static BlockExpression DynamicChunk(int chunk, Expression data, Expression offset, Expression length, Expression hash, Mixer mixer, bool ignoreCase, bool subtractLength = true)
    {
        Expression readCall = GetReadFunc(data, offset, chunk, ignoreCase);

        // hash = mixer(Read(data, offset))
        // offset += chunk
        // length -= chunk
        return subtractLength
            ? Block(
                Assign(hash, mixer(hash, readCall)),
                AddAssign(offset, Constant(chunk)),
                SubtractAssign(length, Constant(chunk))
            )
            : Block(
                Assign(hash, mixer(hash, readCall)),
                AddAssign(offset, Constant(chunk))
            );
    }

    /// <summary>Builds a call to the generated unaligned read helper for a 1, 2, 4, or 8 byte chunk.</summary>
    private static Expression GetReadFunc(Expression data, Expression idx, int length, bool ignoreCase)
    {
        GeneratorFunction func = length switch
        {
            1 => GeneratorFunction.ReadU8,
            2 => GeneratorFunction.ReadU16,
            4 => GeneratorFunction.ReadU32,
            8 => GeneratorFunction.ReadU64,
            _ => throw new InvalidOperationException($"Invalid length: {length.ToString(NumberFormatInfo.InvariantInfo)}")
        };

        MethodInfo? readFunc = typeof(GeneratorFunctions).GetMethod(func.ToString(), BindingFlags.Static | BindingFlags.Public);

        if (readFunc == null)
            throw new InvalidOperationException("Could not find method");

        // Read(data, idx)
        Expression read = Convert(Call(readFunc, data, idx), typeof(ulong));

        if (ignoreCase)
        {
            // Apply ASCII case folding by setting bit 5 in every byte: read | mask.
            // This maps A-Z to a-z. Non-letter bytes are also affected, but since the
            // same transformation is applied at both build time and lookup time, the hash
            // values remain consistent.
            ulong mask = length switch
            {
                1 => 0x20UL,
                2 => 0x2020UL,
                4 => 0x20202020UL,
                8 => 0x2020202020202020UL,
                _ => throw new InvalidOperationException("Invalid size")
            };

            read = Or(read, Constant(mask));
        }

        return read;
    }
}