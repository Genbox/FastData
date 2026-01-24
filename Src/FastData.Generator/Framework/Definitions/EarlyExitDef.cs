using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits;

namespace Genbox.FastData.Generator.Framework.Definitions;

public abstract class EarlyExitDef : IEarlyExitDef
{
    protected abstract bool IsEnabled { get; }

    public string GetEarlyExits<T>(IEnumerable<IEarlyExit> earlyExits, MethodType methodType, bool ignoreCase, GeneratorEncoding encoding, SharedCode shared)
    {
        if (!IsEnabled)
            return string.Empty;

        StringBuilder sb = new StringBuilder();

        foreach (IEarlyExit spec in earlyExits)
        {
            if (spec is LengthEqualEarlyExit(var length, var byteCount))
                sb.AppendLine(GetLengthEqualEarlyExit(methodType, length, byteCount, encoding));
            else if (spec is LengthRangeEarlyExit(var minLength, var maxLength, var minByteCount, var maxByteCount))
                sb.AppendLine(GetLengthRangeEarlyExit(methodType, minLength, maxLength, minByteCount, maxByteCount, encoding));
            else if (spec is LengthBitmapEarlyExit(var bitSet))
                sb.AppendLine(GetLengthBitmapEarlyExit(methodType, bitSet));
            else if (spec is LengthDivisorEarlyExit(var charDivisor, var byteDivisor))
                sb.AppendLine(GetLengthDivisorEarlyExit(methodType, charDivisor, byteDivisor));
            else if (spec is CharRangeEarlyExit(var rangePosition, var min, var max))
                sb.AppendLine(GetCharRangeEarlyExit(methodType, rangePosition, min, max, ignoreCase, encoding));
            else if (spec is CharEqualsEarlyExit(var equalsPosition, var value))
                sb.AppendLine(GetCharEqualsEarlyExit(methodType, equalsPosition, value, ignoreCase, encoding));
            else if (spec is CharBitmapEarlyExit(var bitmapPosition, var low, var high))
                sb.AppendLine(GetCharBitmapEarlyExit(methodType, bitmapPosition, low, high, ignoreCase, encoding));
            else if (spec is StringBitMaskEarlyExit(var stringMask, var stringByteCount))
                sb.AppendLine(GetStringBitMaskEarlyExit(methodType, stringMask, stringByteCount, ignoreCase, encoding));
            else if (spec is StringPrefixSuffixEarlyExit(var prefix, var suffix))
                sb.AppendLine(GetStringPrefixSuffixEarlyExit(methodType, prefix, suffix, ignoreCase));
            else if (spec is ValueRangeEarlyExit<T>(var minValue, var maxValue))
                sb.AppendLine(GetValueEarlyExit(methodType, minValue, maxValue));
            else if (spec is ValueBitMaskEarlyExit(var mask))
                sb.AppendLine(GetValueBitMaskEarlyExit<T>(methodType, mask));
            else
                throw new InvalidOperationException("Unknown early exit type: " + spec.GetType().Name);
        }

        return sb.ToString();
    }

    protected abstract string GetLengthEqualEarlyExit(MethodType methodType, uint length, uint byteCount, GeneratorEncoding encoding);
    protected abstract string GetLengthRangeEarlyExit(MethodType methodType, uint min, uint max, uint minByte, uint maxByte, GeneratorEncoding encoding);
    protected abstract string GetLengthBitmapEarlyExit(MethodType methodType, ulong[] bitSet);
    protected abstract string GetLengthDivisorEarlyExit(MethodType methodType, uint charDivisor, uint byteDivisor);
    protected abstract string GetCharRangeEarlyExit(MethodType methodType, CharPosition position, char min, char max, bool ignoreCase, GeneratorEncoding encoding);
    protected abstract string GetCharEqualsEarlyExit(MethodType methodType, CharPosition position, char value, bool ignoreCase, GeneratorEncoding encoding);
    protected abstract string GetCharBitmapEarlyExit(MethodType methodType, CharPosition position, ulong low, ulong high, bool ignoreCase, GeneratorEncoding encoding);
    protected abstract string GetStringBitMaskEarlyExit(MethodType methodType, ulong mask, int byteCount, bool ignoreCase, GeneratorEncoding encoding);
    protected abstract string GetStringPrefixSuffixEarlyExit(MethodType methodType, string prefix, string suffix, bool ignoreCase);
    protected abstract string GetValueBitMaskEarlyExit<T>(MethodType methodType, ulong mask);
    protected abstract string GetValueEarlyExit<T>(MethodType methodType, T min, T max);
}