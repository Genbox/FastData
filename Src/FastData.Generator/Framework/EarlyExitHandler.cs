using System.Text;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Specs.EarlyExit;

namespace Genbox.FastData.Generator.Framework;

public abstract class EarlyExitHandler : IEarlyExitHandler
{
    protected abstract bool IsEnabled { get; }

    public string GetEarlyExits<T>(IEnumerable<IEarlyExit> earlyExits)
    {
        if (!IsEnabled)
            return string.Empty;

        StringBuilder sb = new StringBuilder();

        foreach (IEarlyExit spec in earlyExits)
        {
            if (spec is MinMaxLengthEarlyExit(var minLength, var maxLength))
                sb.Append(GetLengthEarlyExits(minLength, maxLength));
            else if (spec is MinMaxValueEarlyExit<T>(var minValue, var maxValue))
                sb.Append(GetValueEarlyExits(minValue, maxValue));
            else if (spec is LengthBitSetEarlyExit(var bitSet))
                sb.Append(GetMaskEarlyExit(bitSet));
            else
                throw new InvalidOperationException("Unknown early exit type: " + spec.GetType().Name);
        }

        return sb.ToString();
    }

    protected abstract string GetMaskEarlyExit(ulong bitSet);
    protected abstract string GetValueEarlyExits<T>(T min, T max);
    protected abstract string GetLengthEarlyExits(uint min, uint max);
}