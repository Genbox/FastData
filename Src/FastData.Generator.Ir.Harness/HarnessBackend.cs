using Genbox.FastData.Generator.Ir.Emission;
using Genbox.FastData.Generator.Ir.Prepared;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;

namespace Genbox.FastData.Generator.Ir.Harness;

/// <summary>Supplies a language toolchain and runtime syntax for shared harness programs.</summary>
public abstract class HarnessBackend
{
    /// <summary>Gets the language name used by the command-line selector.</summary>
    public abstract string Name { get; }

    /// <summary>Gets the pinned Docker image.</summary>
    public abstract string Image { get; }

    /// <summary>Gets the generated source file extension.</summary>
    public abstract string Extension { get; }

    /// <summary>Creates the compilation command.</summary>
    public abstract string BuildCommand(string id, bool benchmark);

    /// <summary>Creates the execution command.</summary>
    public abstract string RunCommand(string id, string arguments);

    /// <summary>Creates the lookup generator for this language.</summary>
    public abstract IrCodeGenerator Generator();

    /// <summary>Renders a test harness for the supplied lookup and cases.</summary>
    public string RenderTests(IrSourceEmitter emitter, IrArrayLiteralBase keys, IrArrayLiteralBase? values, IrArrayLiteralBase misses, IrArrayLiteralBase? alternateKeys = null) =>
        Emit(emitter, TestHarnessBuilder.Build(emitter.Lookup, keys, values, misses, alternateKeys), false);

    /// <summary>Renders a benchmark harness for the supplied lookup and queries.</summary>
    public string RenderBenchmark(IrSourceEmitter emitter, IrArrayLiteralBase queries) =>
        Emit(emitter, BenchmarkHarnessBuilder.Build(queries), true);

    private string Emit(IrSourceEmitter lookup, HarnessProgram program, bool benchmark)
    {
        IrType keyType = lookup.Lookup.Lookup.KeyType;
        PreparedEmissionPlan plan = PreparedEmissionPlan.Create(program.Data, program.Functions);
        IrSourceEmitter emitter = lookup.CreateCompanion(plan, Render);
        SourceWriter writer = new SourceWriter();
        writer.Line(benchmark ? Runtime() : ApiAdapters(emitter));
        writer.Line(emitter.DataDeclarations());
        writer.Line(emitter.FunctionDeclarations());
        return Wrap(writer.ToString(), benchmark);

        string Render(IrExpression expression, string[] arguments) => expression switch
        {
            HarnessData data => lookup.MemberReference(data.Name),
            HarnessCall call => Operation(keyType, call.Operation, arguments),
            HarnessFunctionCall call => $"{lookup.MemberReference(call.Name)}({string.Join(", ", arguments)})",
            _ => throw new NotSupportedException($"Unsupported harness expression '{expression.GetType().Name}'.")
        };
    }

    /// <summary>Renders a harness operation using the lookup's key type.</summary>
    protected abstract string Operation(IrType keyType, HarnessOperation operation, ReadOnlySpan<string> arguments);

    /// <summary>Indents and wraps members in the language's harness scope and entry point.</summary>
    protected abstract string Wrap(string members, bool benchmark);

    /// <summary>Renders timing, optimizer barriers, and reporting helpers.</summary>
    protected abstract string Runtime();

    /// <summary>Renders public lookup API adapters.</summary>
    protected abstract string ApiAdapters(IrSourceEmitter emitter);
}