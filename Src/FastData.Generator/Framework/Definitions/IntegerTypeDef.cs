using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Framework.Interfaces;

namespace Genbox.FastData.Generator.Framework.Definitions;

public class IntegerTypeDef<T>(string name, T minValue, T maxValue, string minValueStr, string maxValueStr, Func<T, string>? print = null) : ITypeDef<T> where T : notnull
{
    public DataType DataType => (DataType)Type.GetTypeCode(typeof(T));
    public string Name { get; } = name;
    public Func<T, string> Print { get; } = x => PrintInternal(x, minValue, minValueStr, maxValue, maxValueStr, print);
    public Func<object, string> PrintObj { get; } = x => PrintInternal((T)x, minValue, minValueStr, maxValue, maxValueStr, print);

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