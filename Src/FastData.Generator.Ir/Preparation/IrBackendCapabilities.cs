using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Ir.Operations;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Generator.Ir.Preparation;

/// <summary>Text representation and operations supported by an IR backend.</summary>
public sealed record IrBackendCapabilities(GeneratorEncoding Encoding, IrBuiltinOperation SupportedBuiltins, IrSwitchKinds SwitchKinds = IrSwitchKinds.None)
{
    /// <summary>The minimum width of native integral arithmetic results. Zero preserves each operand's width.</summary>
    public int IntegerPromotionWidth { get; init; }

    /// <summary>Native operations that also need portable helper functions for runtime-dependent fallbacks.</summary>
    public IrBuiltinOperation RuntimeFallbacks { get; init; }

    /// <summary>Marks unsigned addition, subtraction, and multiplication for explicit wrapping emission on targets whose native arithmetic can trap or requires wrapping syntax.</summary>
    public bool ExplicitWrappingArithmetic { get; init; }

    /// <summary>The corresponding portable text representation. Frontend positions must count code units.</summary>
    public IrTextEncoding TextEncoding => Encoding switch
    {
        GeneratorEncoding.AsciiBytes or GeneratorEncoding.Utf8Bytes => IrTextEncoding.Utf8,
        GeneratorEncoding.Utf16CodeUnits => IrTextEncoding.Utf16,
        _ => throw new NotSupportedException($"Frontend encoding '{Encoding}' has no supported IR code-unit mapping.")
    };
}