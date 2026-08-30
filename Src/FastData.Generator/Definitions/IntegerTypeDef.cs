using System.Globalization;
using Genbox.FastData.Generator.Abstracts;

namespace Genbox.FastData.Generator.Definitions;

/// <summary>Describes the target-language representation of an integral type.</summary>
/// <typeparam name="T">The CLR integral type.</typeparam>
/// <param name="name">The target-language type name.</param>
/// <param name="minValue">The minimum value of the CLR type.</param>
/// <param name="maxValue">The maximum value of the CLR type.</param>
/// <param name="minValueStr">The target-language literal for <paramref name="minValue"/>.</param>
/// <param name="maxValueStr">The target-language literal for <paramref name="maxValue"/>.</param>
/// <param name="print">An optional function that formats other values.</param>
public class IntegerTypeDef<T>(string name, T minValue, T maxValue, string minValueStr, string maxValueStr, Func<T, string>? print = null) : ITypeDef<T>
{
    /// <inheritdoc />
    public TypeCode KeyType => Type.GetTypeCode(typeof(T));

    /// <inheritdoc />
    public string Name { get; } = name;

    /// <inheritdoc />
    public Func<TypeMap, T, string> Print { get; } = (_, x) => PrintInternal(x, minValue, minValueStr, maxValue, maxValueStr, print);

    /// <inheritdoc />
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