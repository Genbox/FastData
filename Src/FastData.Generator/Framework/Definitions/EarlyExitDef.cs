using System.Text;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits;

namespace Genbox.FastData.Generator.Framework.Definitions;

public abstract class EarlyExitDef : IEarlyExitDef
{
    protected abstract bool IsEnabled { get; }

    public string GetEarlyExits<T>(IEnumerable<IEarlyExit> earlyExits, MethodType methodType, bool ignoreCase)
    {
        if (!IsEnabled)
            return string.Empty;

        StringBuilder sb = new StringBuilder();

        foreach (IEarlyExit spec in earlyExits)
        {
            if (spec is MinMaxLengthEarlyExit(var minLength, var maxLength, var minByteCount, var maxByteCount))
                sb.AppendLine(GetLengthEarlyExit(methodType, minLength, maxLength, minByteCount, maxByteCount));
            else if (spec is MinMaxValueEarlyExit<T>(var minValue, var maxValue))
                sb.AppendLine(GetValueEarlyExit(methodType, minValue, maxValue));
            else if (spec is ValueBitMaskEarlyExit(var mask))
                sb.AppendLine(GetValueBitMaskEarlyExit<T>(methodType, mask));
            else if (spec is LengthBitSetEarlyExit(var bitSet))
                sb.AppendLine(GetMaskEarlyExit(methodType, bitSet));
            else if (spec is PrefixSuffixEarlyExit(var prefix, var suffix))
                sb.AppendLine(GetPrefixSuffixEarlyExit(methodType, prefix, suffix, ignoreCase));
            else
                throw new InvalidOperationException("Unknown early exit type: " + spec.GetType().Name);
        }

        return sb.ToString();
    }

    protected abstract string GetMaskEarlyExit(MethodType methodType, ulong[] bitSet);
    protected abstract string GetValueEarlyExit<T>(MethodType methodType, T min, T max);
    protected abstract string GetValueBitMaskEarlyExit<T>(MethodType methodType, ulong mask);
    protected abstract string GetLengthEarlyExit(MethodType methodType, uint min, uint max, uint minByte, uint maxByte);
    protected abstract string GetPrefixSuffixEarlyExit(MethodType methodType, string prefix, string suffix, bool ignoreCase);
}