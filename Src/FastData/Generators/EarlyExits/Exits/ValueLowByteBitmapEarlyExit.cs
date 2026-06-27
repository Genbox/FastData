using System.Linq.Expressions;
using System.Numerics;
using Genbox.FastData.Generators.Abstracts;
using static Genbox.FastData.Generators.Helpers.TypeHelper;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// Rejects integral values whose low byte is absent from the observed key set.
// The filter represents all 8 low bits: 256 buckets stored as four 64-bit words.
public sealed record ValueLowByteBitmapEarlyExit(ulong Word0, ulong Word1, ulong Word2, ulong Word3) : IEarlyExit
{
    public float AcceptedDensity => (BitOperations.PopCount(Word0) + BitOperations.PopCount(Word1) + BitOperations.PopCount(Word2) + BitOperations.PopCount(Word3)) / 256f;

    public Expression GetExpression(ParameterExpression key)
    {
        Type keyType = key.Type;
        Type unsignedType = GetUnsignedType(keyType);
        Expression keyValue = keyType == unsignedType ? key : Convert(key, unsignedType);
        object lowByteMask = ConvertValueToType(0xffUL, unsignedType);
        Expression bucket = Convert(And(keyValue, Constant(lowByteMask, unsignedType)), typeof(int));

        // bucket is 0..255. The top two bucket bits select one of four ulong words;
        // the lower six bucket bits select the bit inside that 64-bit word.
        Expression wordIndex = RightShift(bucket, Constant(6));
        Expression bitOffset = And(bucket, Constant(63));
        Expression bit = LeftShift(Constant(1UL), bitOffset);

        Expression? result = null;
        ulong[] words = [Word0, Word1, Word2, Word3];

        for (int i = 0; i < words.Length; i++)
        {
            if (words[i] == ulong.MaxValue)
                continue;

            Expression test = Equal(wordIndex, Constant(i));
            Expression absent = Equal(And(Constant(words[i]), bit), Constant(0UL));
            Expression branch = AndAlso(test, absent);
            result = result == null ? branch : OrElse(result, branch);
        }

        return result ?? Constant(false);
    }

    public bool IsWorseThan(IEarlyExit other) => false;

    public ulong KeyspaceSize => 256UL - (ulong)(BitOperations.PopCount(Word0) + BitOperations.PopCount(Word1) + BitOperations.PopCount(Word2) + BitOperations.PopCount(Word3));
}