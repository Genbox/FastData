using Genbox.FastData.Generator.Ir.Operations;
using Genbox.FastData.Generator.Ir.Preparation;
using Genbox.FastData.Generator.Ir.Prepared;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Generator.Ir.Emission;

/// <summary>Traverses prepared expressions and manages source emission for a lookup and its companion programs.</summary>
public abstract class IrSourceEmitter(PreparedLookup lookup) : StatementWriter
{
    private DataPreparation _data = new DataPreparation();

    protected abstract LiteralFormatter Literals { get; }
    public PreparedLookup Lookup { get; } = lookup;
    public SourceWriter Writer { get; } = new SourceWriter();
    protected IReadOnlyList<string> Variables { get; set; } = [];

    /// <summary>Renders extension expressions in appended functions, including harness runtime operations.</summary>
    /// <remarks>Custom expressions must reference aggregate data emitted separately through <see cref="EmitData" />.</remarks>
    public Func<IrExpression, string>? CustomExpression { get; set; }

    public abstract string Type(IrType type);
    public string Literal(IrLiteral literal) => Literals.Literal(literal);

    public void EmitData(PreparedData data)
    {
        BindLiteralReferences();
        _data.Add(data);
        _data.Flush(WriteData);
    }

    private void BindLiteralReferences() => Literals.Reference = literal => _data.Reference(literal) is {} name ? MemberReference(name) : null;

    protected string Complete(Action<IrSourceEmitter>? append)
    {
        if (append != null)
        {
            BeginCompanion();
            append(this);
        }

        return Writer.ToString();
    }

    private void BeginCompanion()
    {
        // Companion declarations occupy a separate scope and own their literal storage.
        _data = new DataPreparation();
        Variables = [];
    }

    protected void EmitDeclarations()
    {
        foreach (PreparedData data in Lookup.Data)
            EmitData(data);
    }

    protected void EmitFunctions()
    {
        foreach (PreparedFunction function in Lookup.Functions)
            EmitFunction(function);
    }

    public void EmitFunction(PreparedFunction function)
    {
        BindLiteralReferences();
        _data.Visit(function.Body);
        _data.Flush(WriteData);
        IReadOnlyList<string> previous = Variables;
        Variables = function.VariableNames;
        OpenFunction(function, string.Join(", ", function.Parameters.Select(Parameter)));

        foreach (PreparedDeclaration local in function.Locals)
            WriteLocal(local);

        Statements(function.Body);
        CloseFunction();
        Variables = previous;
    }

    public string Expression(IrExpression expression) => expression switch
    {
        IrLiteral literal => Literal(literal),
        IrVariableExpression variable => Variables[variable.VariableId.Value],
        IrDataExpression data => MemberReference(Lookup.DataName(data.DataId)),
        IrIndexExpression index => Index(Expression(index.Collection), Expression(index.Index)),
        IrFieldExpression field => Field(Expression(field.Receiver), Lookup.FieldName(field.FieldId)),
        IrLogicalExpression logical => Logical(logical.Operation, Expression(logical.Left), Expression(logical.Right)),
        IrBinaryExpression binary => Binary(binary.Operation, Expression(binary.Left), Expression(binary.Right)),
        IrConversionExpression conversion => Convert(conversion.Type, Expression(conversion.Operand)),
        PreparedNullCheck check => NullCheck(Expression(check.Operand), check.IsNull),
        PreparedWrappingArithmetic arithmetic => Wrapping(arithmetic, Expression(arithmetic.Left), Expression(arithmetic.Right)),
        PreparedStringComparison comparison => StringComparison(comparison.Operation, Expression(comparison.Left), Expression(comparison.Right)),
        PreparedBitCast cast => BitCast(cast, Expression(cast.Operand)),
        PreparedBuiltinCall call => Builtin(call.Operation, Arguments(call.Arguments)),
        PreparedFunctionCall call => Call(call.Name, Arguments(call.Arguments)),
        _ => CustomExpression?.Invoke(expression) ?? throw new NotSupportedException($"Unsupported expression '{expression.GetType().Name}'.")
    };

    private string[] Arguments(IReadOnlyList<IrExpression> arguments)
    {
        string[] result = new string[arguments.Count];
        for (int i = 0; i < result.Length; i++)
            result[i] = Expression(arguments[i]);
        return result;
    }

    protected virtual string MemberReference(string name) => name;
    protected abstract string Index(string collection, string index);
    protected abstract string Field(string receiver, string name);
    protected abstract string Binary(BinaryOperation operation, string left, string right);
    protected abstract string Logical(LogicalOperation operation, string left, string right);
    protected abstract string Convert(IrType type, string operand);
    protected abstract string NullCheck(string operand, bool isNull);
    protected abstract string Wrapping(PreparedWrappingArithmetic arithmetic, string left, string right);
    protected abstract string StringComparison(BinaryOperation operation, string left, string right);
    protected abstract string BitCast(PreparedBitCast cast, string operand);
    protected abstract string Builtin(IrBuiltinOperation operation, string[] arguments);
    protected abstract string Call(string name, string[] arguments);

    protected abstract void WriteData(PreparedData data);

    protected abstract string Parameter(PreparedDeclaration parameter);
    protected abstract void OpenFunction(PreparedFunction function, string parameters);
    protected abstract void WriteLocal(PreparedDeclaration local);
    protected abstract void CloseFunction();
}