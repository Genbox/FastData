using System.Text;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits;

namespace Genbox.FastData.Generator.Framework.Definitions;

public abstract class EarlyExitDef : IEarlyExitDef
{
    protected abstract bool IsEnabled { get; }

    public string GetEarlyExits<T>(IEnumerable<IEarlyExit> earlyExits, MethodType methodType)
    {
        if (!IsEnabled)
            return string.Empty;

        StringBuilder sb = new StringBuilder();

        foreach (IEarlyExit spec in earlyExits)
        {
            if (spec is MinMaxLengthEarlyExit(var minLength, var maxLength, var minByteCount, var maxByteCount))
                sb.Append(GetLengthEarlyExits(methodType, minLength, maxLength, minByteCount, maxByteCount));
            else if (spec is MinMaxValueEarlyExit<T>(var minValue, var maxValue))
                sb.Append(GetValueEarlyExits(methodType, minValue, maxValue));
            else if (spec is LengthBitSetEarlyExit(var bitSet))
                sb.Append(GetMaskEarlyExit(methodType, bitSet));
            else
                throw new InvalidOperationException("Unknown early exit type: " + spec.GetType().Name);
        }

        return sb.ToString();
    }

    protected abstract string GetMaskEarlyExit(MethodType methodType, ulong bitSet);
    protected abstract string GetValueEarlyExits<T>(MethodType methodType, T min, T max);
    protected abstract string GetLengthEarlyExits(MethodType methodType, uint min, uint max, uint minByte, uint maxByte);
}