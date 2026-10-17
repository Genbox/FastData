using JetBrains.Annotations;

namespace Genbox.FastData.Ir.Validation;

/// <summary>Reports a validation failure at a specific IR declaration or node path.</summary>
[PublicAPI]
public sealed class IrValidationException : InvalidOperationException
{
    /// <summary>Creates a validation error with a default message.</summary>
    public IrValidationException() : this("IR validation failed.") {}

    /// <summary>Creates a validation error with a message.</summary>
    public IrValidationException(string message) : base(message) {}

    /// <summary>Creates a validation error with a message and underlying cause.</summary>
    public IrValidationException(string message, Exception innerException) : base(message, innerException) {}

    /// <summary>Creates a structured validation error at the program root.</summary>
    public IrValidationException(IrDiagnosticCode code, string message) : base(message)
    {
        Code = code;
    }

    /// <summary>Creates a validation error at a specific program path.</summary>
    public IrValidationException(string path, string message, Exception? innerException = null, IrDiagnosticCode code = IrDiagnosticCode.InvalidProgram) : base($"IR validation failed at '{path}': {message}", innerException)
    {
        Path = path;
        Code = code;
    }

    /// <summary>The path to the invalid declaration or node.</summary>
    public string Path { get; } = "Program";

    /// <summary>The stable category of the failure.</summary>
    public IrDiagnosticCode Code { get; } = IrDiagnosticCode.InvalidProgram;
}