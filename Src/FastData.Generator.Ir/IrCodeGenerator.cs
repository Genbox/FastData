using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Ir.Emission;
using Genbox.FastData.Generator.Ir.Lowering;
using Genbox.FastData.Generator.Ir.Model;
using Genbox.FastData.Generator.Ir.Operations;
using Genbox.FastData.Generator.Ir.Preparation;
using Genbox.FastData.Generator.Ir.Prepared;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Generator.Ir;

/// <summary>Prepares a lookup and renders it through a language backend.</summary>
public abstract class IrCodeGenerator(IrCodeGeneratorConfig config) : ICodeGenerator
{
    protected IrCodeGeneratorConfig Config { get; } = config;
    public abstract IrBuiltinOperation SupportedBuiltins { get; }

    /// <summary>The corresponding portable text representation. Frontend positions must count code units.</summary>
    internal IrTextEncoding TextEncoding => Encoding switch
    {
        GeneratorEncoding.AsciiBytes or GeneratorEncoding.Utf8Bytes => IrTextEncoding.Utf8,
        GeneratorEncoding.Utf16CodeUnits => IrTextEncoding.Utf16,
        _ => throw new NotSupportedException($"Frontend encoding '{Encoding}' has no supported IR code-unit mapping.")
    };

    public GeneratorEncoding Encoding => Config.Encoding;

    public string Generate<TKey, TValue>(GeneratorConfigBase genCfg, IContext context) => CreateEmitter<TKey, TValue>(genCfg, context).Emit();

    public IrSourceEmitter CreateEmitter<TKey, TValue>(GeneratorConfigBase genCfg, IContext context) => CreateEmitter(Prepare<TKey, TValue>(genCfg, context));

    /// <summary>Prepares a directly built lookup and creates its language renderer.</summary>
    public IrSourceEmitter CreateEmitter(FastDataProgram lookup) => CreateEmitter(Prepare(lookup));

    /// <summary>Creates a language renderer for a prepared lookup.</summary>
    protected abstract IrSourceEmitter CreateEmitter(PreparedLookup lookup);

    /// <summary>Builds a prepared lookup for the supplied backend, preserving a validated core program for reference execution.</summary>
    protected PreparedLookup Prepare<TKey, TValue>(GeneratorConfigBase config, IContext context)
    {
        if (config is StringGeneratorConfig text && text.Encoding != Encoding)
            throw new ArgumentException("Frontend and backend text encodings must match.", nameof(config));

        LoweredLookup lowered = ContextLowerer.Lower<TKey, TValue>(config, context, TextEncoding);
        return Prepare(lowered, config.StructureType.ToString());
    }

    /// <summary>Prepares a validated lookup using the target's text encoding and builtin support.</summary>
    protected PreparedLookup Prepare(FastDataProgram lookup)
    {
        if (lookup.Program.TextEncoding != TextEncoding)
            throw new ArgumentException("Program and backend text encodings must match.", nameof(lookup));

        return Prepare(new LoweredLookup(lookup, null, null), "Custom");
    }

    private PreparedLookup Prepare(LoweredLookup lowered, string structureName)
    {
        (IReadOnlyList<PreparedFunction> functions, IReadOnlyList<PreparedCall> calls, IrBuiltinOperation required) = FunctionPreparation.Run(lowered.Lookup.Program, "fdFunction", SupportedBuiltins);
        return new PreparedLookup(lowered.Lookup, functions, calls, required, structureName, Encoding, lowered.KeyStorage, lowered.ValueStorage);
    }
}