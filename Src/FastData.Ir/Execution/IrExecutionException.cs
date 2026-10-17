namespace Genbox.FastData.Ir.Execution;

/// <summary>Reports an interpreter execution failure, including invalid accesses, signed overflow, invalid shifts, character conversion failures, and missing external handlers.</summary>
/// <remarks>Integral division by zero is reported as <see cref="DivideByZeroException" />.</remarks>
public sealed class IrExecutionException : InvalidOperationException
{
    /// <summary>Creates an execution error with a default message.</summary>
    public IrExecutionException() : this("IR execution failed.") {}

    /// <summary>Creates an execution error with a message.</summary>
    public IrExecutionException(string message) : base(message) {}

    /// <summary>Creates an execution error with a message and underlying cause.</summary>
    public IrExecutionException(string message, Exception innerException) : base(message, innerException) {}
}