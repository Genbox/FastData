using System.Linq.Expressions;

namespace Genbox.FastData.Generator.Lowered;

/// <summary>An assignment operation and its assigned local, when applicable.</summary>
public sealed class LoweredAssignmentStatement(ValidatedExpression operation, ParameterExpression? assignedLocal) : LoweredStatement
{
    /// <summary>Gets the validated assignment operation.</summary>
    public ValidatedExpression Operation { get; } = operation;
    /// <summary>Gets the assigned local, or <see langword="null" /> for other assignment targets.</summary>
    public ParameterExpression? AssignedLocal { get; } = assignedLocal;
}