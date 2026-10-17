namespace Genbox.FastData.Ir.Validation;

/// <summary>Machine-readable validation categories, independent of diagnostic wording. The path and message identify the exact rule.</summary>
public enum IrDiagnosticCode
{
    /// <summary>A structural rule is violated: a missing node or name, an undefined enum value, a malformed literal, or a misplaced break.</summary>
    InvalidProgram,

    /// <summary>A declaration ID refers to a missing declaration.</summary>
    MissingDeclaration,

    /// <summary>Operand, argument, or destination types are incompatible, or a call has the wrong number of arguments.</summary>
    TypeMismatch,

    /// <summary>A variable is read before assignment.</summary>
    UnassignedVariable,

    /// <summary>A function has a reachable fallthrough path.</summary>
    MissingReturn,

    /// <summary>A domain contract has incompatible bindings or signatures.</summary>
    InvalidContract
}