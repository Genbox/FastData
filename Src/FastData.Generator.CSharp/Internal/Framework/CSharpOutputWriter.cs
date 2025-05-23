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

    internal string GetEqualFunction(string var1, string var2)
    {
        if (GeneratorConfig.DataType == DataType.String)
            return $"StringComparer.{GeneratorConfig.StringComparison}.Equals({var1}, {var2})";

        return $"{var1}.Equals({var2})";
    }

    protected override string GetFieldModifier()
    {
        return cfg.ClassType == ClassType.Static ? "private static readonly " : "private readonly ";
    }

    protected override string GetMethodModifier()
    {
        return cfg.ClassType == ClassType.Static ? "public static " : "public ";
    }

    protected override string GetMethodAttributes()
    {
        if (cfg.GeneratorOptions.HasFlag(CSharpOptions.DisableInlining))
            return "[MethodImpl(MethodImplOptions.NoInlining)]";

        if (cfg.GeneratorOptions.HasFlag(CSharpOptions.AggressiveInlining))
            return "[MethodImpl(MethodImplOptions.AggressiveInlining)]";

        return string.Empty;
    }

    protected override string GetModFunction(string variable, ulong value)
    {
        if (value == 0)
            throw new ArgumentOutOfRangeException(nameof(value), "A length of 0 is not valid in a modulus operation");

        // x % 1 = 0
        if (value == 1)
            return "0";

        if (MathHelper.IsPowerOfTwo((uint)value) && !cfg.GeneratorOptions.HasFlag(CSharpOptions.DisableModulusOptimization))
            return $"{variable} & {value - 1}";

        return $"{variable} % {value}";
    }
}