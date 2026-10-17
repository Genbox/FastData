namespace Genbox.FastData.Ir.Validation;

/// <summary>Reports IR contract violations with diagnostic codes and paths.</summary>
internal static class IrValidation
{
    internal static void Require(bool condition, string message, IrDiagnosticPath path = default, IrDiagnosticCode code = IrDiagnosticCode.InvalidProgram)
    {
        if (!condition)
            throw Fail(message, path, code);
    }

    /// <summary>Creates the failure for callers that format their message only after a check fails.</summary>
    internal static IrValidationException Fail(string message, IrDiagnosticPath path = default, IrDiagnosticCode code = IrDiagnosticCode.InvalidProgram)
    {
        if (path.IsEmpty)
            return new IrValidationException(code, message);

        return new IrValidationException(path.ToString(), message, code: code);
    }
}