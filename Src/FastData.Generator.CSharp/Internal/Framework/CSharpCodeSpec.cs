using Genbox.FastData.Generator.CSharp.Enums;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Helpers;

namespace Genbox.FastData.Generator.CSharp.Internal.Framework;

internal class CSharpCodeSpec(CSharpCodeGeneratorConfig cfg) : CodeSpec
{
    public override string GetFieldModifier()
    {
        return cfg.ClassType == ClassType.Static ? "private static readonly " : "private readonly ";
    }

    public override string GetMethodModifier()
    {
        return cfg.ClassType == ClassType.Static ? "public static " : "public ";
    }

    public override string GetMethodAttributes()
    {
        if (cfg.GeneratorOptions.HasFlag(CSharpOptions.DisableInlining))
            return "[MethodImpl(MethodImplOptions.NoInlining)]";

        if (cfg.GeneratorOptions.HasFlag(CSharpOptions.AggressiveInlining))
            return "[MethodImpl(MethodImplOptions.AggressiveInlining)]";

        return string.Empty;
    }

    public override string GetModFunction(string variable, ulong value)
    {
        if (value == 0)
            throw new ArgumentOutOfRangeException(nameof(value), "A length of 0 is not valid in a modulus operation");

        // x % 1 = 0
        if (value == 1)
            return "0";

        if (cfg.GeneratorOptions.HasFlag(CSharpOptions.DisableModulusOptimization))
            return $"{variable} % {value}";

        if (MathHelper.IsPowerOfTwo((uint)value))
            return $"{variable} & {value - 1}";

        ulong multiplier = MathHelper.GetFastModMultiplier((uint)value);
        return $"unchecked((uint)((((({multiplier}ul * {variable}) >> 32) + 1) * {value}) >> 32))";
    }
}