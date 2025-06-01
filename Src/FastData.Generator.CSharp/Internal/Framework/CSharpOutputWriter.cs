using Genbox.FastData.Generator.CSharp.Enums;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Helpers;

namespace Genbox.FastData.Generator.CSharp.Internal.Framework;

internal abstract class CSharpOutputWriter<T>(CSharpCodeGeneratorConfig cfg) : OutputWriter<T>
{
    internal string GetCompareFunction(string var1, string var2)
    {
        if (GeneratorConfig.DataType == DataType.String)
            return $"StringComparer.{GeneratorConfig.StringComparison}.Compare({var1}, {var2})";

        return $"{var1}.CompareTo({var2})";
    }

    protected override string GetEqualFunction(string var1, string var2, DataType dataType = DataType.Null)
    {
        if (dataType == DataType.Null)
            dataType = GeneratorConfig.DataType;

        if (dataType == DataType.String)
            return $"StringComparer.{GeneratorConfig.StringComparison}.Equals({var1}, {var2})";

        return $"{var1} == {var2}";
    }

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