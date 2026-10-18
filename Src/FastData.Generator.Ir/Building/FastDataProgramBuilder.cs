using Genbox.FastData.Generator.Ir.Model;
using Genbox.FastData.Generator.Ir.Operations;
using Genbox.FastData.Generator.Ir.Validation;
using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;
using Genbox.FastData.Ir.Validation;

namespace Genbox.FastData.Generator.Ir.Building;

/// <summary>Builds one slot-index search with optional immutable value storage.</summary>
public sealed class FastDataProgramBuilder
{
    private readonly List<IrDataId> _constants = new List<IrDataId>();
    private readonly IrFunctionBuilder _find;
    private readonly IrType _keyType;

    /// <summary>Declares Find(key), returning an I32 slot or -1 when absent.</summary>
    /// <param name="keyType">The key type.</param>
    /// <param name="textEncoding">The program's text encoding.</param>
    /// <param name="fold">Declares the code-unit fold for String keys, or <see langword="null" /> for ordinal comparison.</param>
    /// <param name="supportedBuiltins">The builtins the target generator implements natively; the rest are emitted as fallback functions.</param>
    /// <exception cref="ArgumentException">The fold requires String keys, or <paramref name="supportedBuiltins" /> contains an invalid capability set.</exception>
    public FastDataProgramBuilder(IrType keyType, IrTextEncoding textEncoding = IrTextEncoding.Utf16, Func<IrProgramBuilder, IrFunctionId>? fold = null, IrBuiltinOperation supportedBuiltins = IrBuiltins.All)
    {
        if (fold != null && keyType != IrType.String)
            throw new ArgumentException("A fold requires String keys.", nameof(fold));

        _keyType = keyType;
        Program = new IrProgramBuilder(textEncoding);
        Builtins = new IrBuiltinBuilder(Program, supportedBuiltins);
        Keys = new LookupKeys(Program, Builtins, keyType, fold?.Invoke(Program));
        _find = Program.Function("Find", IrType.I32);
        KeyParameterId = _find.Parameter("key", keyType);
    }

    /// <summary>The underlying generic builder.</summary>
    public IrProgramBuilder Program { get; }

    /// <summary>The lookup builtin catalog builder.</summary>
    public IrBuiltinBuilder Builtins { get; }

    /// <summary>Encoding-aware text helpers.</summary>
    public LookupText Text => Builtins.Text;

    /// <summary>Key comparison and hashing helpers.</summary>
    public LookupKeys Keys { get; }

    /// <summary>The search function's identity, available before defining its body.</summary>
    public IrFunctionId FindFunctionId => _find.Id;

    /// <summary>The search's key parameter.</summary>
    public IrVariableId KeyParameterId { get; }

    /// <summary>Optional value storage indexed by the search result.</summary>
    public LookupValues? Values { get; set; }

    /// <summary>An optional key and value enumeration.</summary>
    public LookupEnumeration? Enumeration { get; set; }

    /// <summary>Declares a local variable in the search function.</summary>
    public IrVariableId FindLocal(string name, IrType type) => _find.Local(name, type);

    /// <summary>Publishes a named immutable constant.</summary>
    public IrDataId ExportConstant(string name, IrLiteral value)
    {
        IrDataId id = Program.AddData(name, value);
        _constants.Add(id);
        return id;
    }

    /// <summary>Defines the search body with its NaN guard, then validates and freezes the lookup program.</summary>
    /// <remarks>Building completes this builder; call this method once after declaring locals, helpers, and exports.</remarks>
    public FastDataProgram Build(IrBlock body)
    {
        // Ordering helpers assume non-NaN keys; reject NaN before any search strategy can report a hit.
        if (_keyType.IsFloatingPoint)
        {
            IrExpression key = Read(KeyParameterId);
            body = Block(body.Statements.Prepend(If(NotEqual(key, key), Return(I32(-1)))).ToArray());
        }

        _find.Define(body);
        IrProgram program = Program.Build();

        if (Keys.FoldFunctionId is {} fold)
        {
            IrFunction function = program.Resolve(fold);
            IrType unit = program.Text.CodeUnitType;
            Require(function.ReturnType == unit && function.Parameters.Count == 1 && function.Parameters[0].Type == unit, "A fold must take and return the program's code-unit type.", code: IrDiagnosticCode.InvalidContract);
        }

        return FastDataValidator.Validate(program, new LookupContract(FindFunctionId, Values, Enumeration, _constants.ToArray()));
    }
}