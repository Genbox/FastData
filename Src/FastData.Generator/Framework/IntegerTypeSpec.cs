using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generator.Framework.Interfaces.Specs;

namespace Genbox.FastData.Generator.Framework;

public class IntegerTypeSpec<T>(string name, T minValue, T maxValue, string minValueStr, string maxValueStr, Func<T, string>? print = null, IntegerTypeFlags flags = IntegerTypeFlags.None)
    : ITypeSpec<T>, IIntegerTypeSpec where T : notnull
{
    public DataType DataType => (DataType)Type.GetTypeCode(typeof(T));
    public string Name { get; } = name;
    public IntegerTypeFlags Flags { get; } = flags;
    public Func<T, string> Print { get; } = x => PrintInternal(x, minValue, minValueStr, maxValue, maxValueStr, print);

    private static string PrintInternal(T value, T MinValue, string minValueStr, T MaxValue, string maxValueStr, Func<T, string>? print)
    {
        if (value.Equals(MinValue))
            return minValueStr;

        if (value.Equals(MaxValue))
            return maxValueStr;

        if (print != null)
            return print(value);

        return value.ToString();
    }
}