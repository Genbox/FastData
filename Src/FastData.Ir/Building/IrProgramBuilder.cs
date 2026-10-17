using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;
using Genbox.FastData.Ir.Validation;

namespace Genbox.FastData.Ir.Building;

/// <summary>Builds immutable data and declarations with program-local identities.</summary>
public sealed class IrProgramBuilder
{
    private readonly List<(IrFunctionBuilder? Function, IrExternalFunction? External)> _callables = new List<(IrFunctionBuilder?, IrExternalFunction?)>();
    private readonly List<IrData> _data = new List<IrData>();
    private readonly List<RecordDeclaration> _records = new List<RecordDeclaration>();

    /// <summary>Selects the program's text encoding.</summary>
    public IrProgramBuilder(IrTextEncoding textEncoding = IrTextEncoding.Utf16)
    {
        Text = new IrText(textEncoding);
    }

    /// <summary>Generation-time text literal helpers.</summary>
    public IrText Text { get; }

    /// <summary>Declares immutable program data.</summary>
    public IrDataId AddData(string name, IrLiteral value)
    {
        IrDataId id = new IrDataId(_data.Count);
        _data.Add(new IrData(name, value));
        return id;
    }

    /// <summary>Reserves a function identity and creates its declaration owner.</summary>
    public IrFunctionBuilder Function(string name, IrType returnType)
    {
        IrFunctionBuilder function = new IrFunctionBuilder(new IrFunctionId(_callables.Count), name, returnType);
        _callables.Add((function, null));
        return function;
    }

    /// <summary>Declares an opaque external symbol whose calls use ordinary IR call expressions.</summary>
    public IrFunctionId ExternalFunction(string symbol, IrFunctionSignature signature)
    {
        IrFunctionId id = new IrFunctionId(_callables.Count);
        _callables.Add((null, new IrExternalFunction(symbol, signature)));
        return id;
    }

    /// <summary>Declares a nominal record before its fields.</summary>
    public IrRecordId DeclareRecord(string name)
    {
        IrRecordId id = new IrRecordId(_records.Count);
        _records.Add(new RecordDeclaration(name));
        return id;
    }

    /// <summary>Adds an ordered field to a record.</summary>
    public IrFieldId AddField(IrRecordId recordId, string name, IrType type)
    {
        IrFieldId id = new IrFieldId(recordId, _records[recordId.Value].Fields.Count);
        _records[recordId.Value].Fields.Add(new IrField(name, type));
        return id;
    }

    /// <summary>Freezes and validates the declarations; throws an <see cref="IrValidationException" /> at the first error.</summary>
    public IrProgram Build()
    {
        IReadOnlyList<IrCallable> callables = _callables.Select(static callable => (IrCallable?)callable.Function?.Build() ?? callable.External!).ToArray();
        return new IrProgram(callables, _data.ToArray(), _records.Select(static record => new IrRecord(record.Name, record.Fields.ToArray())).ToArray(), Text.Encoding);
    }

    private sealed class RecordDeclaration(string name)
    {
        public string Name { get; } = name;
        public List<IrField> Fields { get; } = new List<IrField>();
    }
}