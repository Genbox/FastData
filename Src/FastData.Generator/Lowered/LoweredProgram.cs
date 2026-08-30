using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Genbox.FastData.Generator.Lowered;

internal sealed class LoweredProgram(LoweredBlock body, HashSet<ParameterExpression> requiredDefaults)
{
    private readonly HashSet<ParameterExpression> _requiredDefaults = requiredDefaults;

    internal LoweredBlock Body { get; } = body;

    [SuppressMessage("Correctness", "SS004:Implement Equals() and GetHashcode() methods for a type used in a collection.", Justification = "ParameterExpression instances are scoped symbols whose reference identity defines initialization requirements.")]
    internal bool RequiresDefaultInitialization(ParameterExpression local) => _requiredDefaults.Contains(local);
}

internal abstract class LoweredStatement
{
    private protected LoweredStatement()
    {
        // Restrict the closed lowered-statement hierarchy to this assembly.
    }
}

internal sealed class LoweredBlock(IReadOnlyList<ParameterExpression> locals, IReadOnlyList<LoweredStatement> statements) : LoweredStatement
{
    internal IReadOnlyList<ParameterExpression> Locals { get; } = locals;
    internal IReadOnlyList<LoweredStatement> Statements { get; } = statements;
}

internal sealed class LoweredAssignmentStatement(ValidatedExpression operation, ParameterExpression? assignedLocal) : LoweredStatement
{
    internal ValidatedExpression Operation { get; } = operation;
    internal ParameterExpression? AssignedLocal { get; } = assignedLocal;
}

internal sealed class LoweredIfStatement(ValidatedExpression test, LoweredBlock ifTrue, LoweredBlock? ifFalse) : LoweredStatement
{
    internal ValidatedExpression Test { get; } = test;
    internal LoweredBlock IfTrue { get; } = ifTrue;
    internal LoweredBlock? IfFalse { get; } = ifFalse;
}

internal sealed class LoweredWhileStatement(ValidatedExpression test, LoweredBlock body) : LoweredStatement
{
    internal ValidatedExpression Test { get; } = test;
    internal LoweredBlock Body { get; } = body;
}

internal sealed class LoweredReturnStatement(ValidatedExpression? value) : LoweredStatement
{
    internal ValidatedExpression? Value { get; } = value;
}

internal sealed class LoweredBreakStatement : LoweredStatement
{
    internal static LoweredBreakStatement Instance { get; } = new LoweredBreakStatement();

    private LoweredBreakStatement() { }
}

internal sealed class LoweredContinueStatement : LoweredStatement
{
    internal static LoweredContinueStatement Instance { get; } = new LoweredContinueStatement();

    private LoweredContinueStatement() { }
}

internal sealed class ValidatedExpression(Expression source, HashSet<ParameterExpression> localReads)
{
    internal Expression Source { get; } = source;
    internal Type Type => Source.Type;
    internal HashSet<ParameterExpression> LocalReads { get; } = localReads;
}