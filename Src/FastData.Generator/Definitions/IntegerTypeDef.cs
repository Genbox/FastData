using System.Globalization;
using Genbox.FastData.Generator.Abstracts;

namespace Genbox.FastData.Generator.Definitions;

public class IntegerTypeDef<T>(string name, T minValue, T maxValue, string minValueStr, string maxValueStr, Func<T, string>? print = null) : ITypeDef<T>
{
    public TypeCode KeyType => Type.GetTypeCode(typeof(T));
    public string Name { get; } = name;
    public Func<TypeMap, T, string> Print { get; } = (_, x) => PrintInternal(x, minValue, minValueStr, maxValue, maxValueStr, print);
    public Func<TypeMap, object, string> PrintObj { get; } = (_, x) => PrintInternal((T)x, minValue, minValueStr, maxValue, maxValueStr, print);

    private static string PrintInternal(T value, T MinValue, string minValueStr, T MaxValue, string maxValueStr, Func<T, string>? print)
    {
        if (EqualityComparer<T>.Default.Equals(value, MinValue))
            return minValueStr;

        if (EqualityComparer<T>.Default.Equals(value, MaxValue))
            return maxValueStr;

        if (print != null)
            return print(value);

        if (value is IFormattable formattable)
            return formattable.ToString(null, NumberFormatInfo.InvariantInfo);

        return value?.ToString() ?? string.Empty;
    }
}