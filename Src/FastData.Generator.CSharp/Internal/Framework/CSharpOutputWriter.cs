using Genbox.FastData.Generator.CSharp.Enums;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generators.Helpers;
using static Genbox.FastData.Generator.CSharp.Internal.StringHelper;

namespace Genbox.FastData.Generator.CSharp.Internal.Framework;

internal abstract class CSharpOutputWriter<T>(CSharpCodeGeneratorConfig cfg) : OutputWriter<T>
{
    protected string FieldModifier => cfg.ClassType == ClassType.Static ? "private static readonly " : "private readonly ";
    protected string MethodModifier => cfg.ClassType == ClassType.Static ? "public static " : "public ";

    protected string MethodAttribute
    {
        get
        {
            if (cfg.GeneratorOptions.HasFlag(CSharpOptions.DisableInlining))
                return "[MethodImpl(MethodImplOptions.NoInlining)]";

            if (cfg.GeneratorOptions.HasFlag(CSharpOptions.AggressiveInlining))
                return "[MethodImpl(MethodImplOptions.AggressiveInlining)]";

            return string.Empty;
        }
    }

    internal string GetCompareFunction(string var1, string var2)
    {
        if (GeneratorConfig.KeyType == KeyType.String)
            return $"StringComparer.{GetStringComparer(GeneratorConfig.IgnoreCase)}.Compare({var1}, {var2})";

        return $"{var1}.CompareTo({var2})";
    }

    protected override string GetEqualFunctionInternal(string var1, string var2, KeyType keyType)
    {
        if (keyType == KeyType.String)
            return $"StringComparer.{GetStringComparer(GeneratorConfig.IgnoreCase)}.Equals({var1}, {var2})";

        return $"{var1} == {var2}";
    }

    protected override string GetMethodHeader(MethodType methodType)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(base.GetMethodHeader(methodType));

        if (TotalTrimLength != 0)
            sb.Append($"        string {TrimmedKeyName} = {InputKeyName}.Substring({TrimPrefix.Length.ToStringInvariant()}, {InputKeyName}.Length - {TotalTrimLength.ToStringInvariant()});");

        return sb.ToString();
    }

    protected override string GetModFunction(string variable, ulong value)
    {
        if (value == 0)
            throw new ArgumentOutOfRangeException(nameof(value), "A length of 0 is not valid in a modulus operation");

        // x % 1 = 0
        if (value == 1)
            return "0";

        if (MathHelper.IsPowerOfTwo((uint)value) && !cfg.GeneratorOptions.HasFlag(CSharpOptions.DisableModulusOptimization))
            return $"({ArraySizeType})({variable} & {value - 1})";

        return $"({ArraySizeType})({variable} % {value})";
    }
}