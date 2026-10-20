using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;
using Genbox.FastData.Ir.Validation;

namespace Genbox.FastData.Ir.Model;

/// <summary>An immutable program whose construction validates every declaration, expression, and control-flow path.</summary>
public sealed class IrProgram
{
    /// <summary>Takes ownership of the supplied declaration collections and validates the program.</summary>
    /// <remarks>Collections are retained without copying. Callers must not modify them through any alias after construction; their immutable declarations and nodes may be shared.</remarks>
    public IrProgram(IReadOnlyList<IrCallable> callables, IReadOnlyList<IrData>? data = null, IReadOnlyList<IrRecord>? records = null, IrTextEncoding textEncoding = IrTextEncoding.Utf16)
    {
        Callables = callables;
        Data = data ?? [];
        Records = records ?? [];
        Require(Enum.IsDefined(textEncoding), "Unsupported text encoding.", IrDiagnosticPath.Root() + ".TextEncoding");
        Text = new IrText(textEncoding);
        IrProgramValidation.Validate(this);
    }

    /// <summary>Defined functions and external declarations in dense ID order.</summary>
    public IReadOnlyList<IrCallable> Callables { get; }

    public IReadOnlyList<IrData> Data { get; }
    public IReadOnlyList<IrRecord> Records { get; }
    public IrTextEncoding TextEncoding => Text.Encoding;
    public IrText Text { get; }

    public IrData Resolve(IrDataId id) => Resolve(id, default);
    public IrRecord Resolve(IrRecordId id) => Resolve(id, default);
    public IrField Resolve(IrFieldId id) => Resolve(id, default);
    public IrFunction Resolve(IrFunctionId id) => ResolveCallable(id) as IrFunction ?? throw Fail("The declaration is not a defined function.");
    public IrCallable ResolveCallable(IrFunctionId id) => ResolveCallable(id, default);

    internal IrData Resolve(IrDataId id, IrDiagnosticPath path) => Find(Data, id.Value, "data", path);
    internal IrRecord Resolve(IrRecordId id, IrDiagnosticPath path) => Find(Records, id.Value, "record", path);
    internal IrField Resolve(IrFieldId id, IrDiagnosticPath path) => Find(Resolve(id.RecordId, path).Fields, id.Value, "field", path);
    internal IrCallable ResolveCallable(IrFunctionId id, IrDiagnosticPath path) => Find(Callables, id.Value, "function", path);

    private static T Find<T>(IReadOnlyList<T> table, int index, string kind, IrDiagnosticPath path)
    {
        if (index >= 0 && index < table.Count)
            return table[index];

        throw Fail($"No {kind} is declared with ID {index}.", path, IrDiagnosticCode.MissingDeclaration);
    }
}