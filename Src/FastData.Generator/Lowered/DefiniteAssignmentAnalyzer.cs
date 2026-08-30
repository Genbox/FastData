using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Genbox.FastData.Generator.Lowered;

internal static class DefiniteAssignmentAnalyzer
{
    internal static HashSet<ParameterExpression> Analyze(LoweredBlock body)
    {
        HashSet<ParameterExpression> requiredDefaults = new HashSet<ParameterExpression>();
        AnalyzeBlock(body, new HashSet<ParameterExpression>(), requiredDefaults);
        return requiredDefaults;
    }

    private static HashSet<ParameterExpression>? AnalyzeBlock(
        LoweredBlock block,
        HashSet<ParameterExpression> assigned,
        HashSet<ParameterExpression> requiredDefaults)
    {
        HashSet<ParameterExpression>? fallthrough = assigned;
        foreach (LoweredStatement statement in block.Statements)
        {
            if (fallthrough == null)
                break;

            fallthrough = AnalyzeStatement(statement, fallthrough, requiredDefaults);
        }

        return fallthrough;
    }

    [SuppressMessage("Correctness", "SS004:Implement Equals() and GetHashcode() methods for a type used in a collection.", Justification = "ParameterExpression instances are scoped symbols whose reference identity defines assignment.")]
    private static HashSet<ParameterExpression>? AnalyzeStatement(
        LoweredStatement statement,
        HashSet<ParameterExpression> assigned,
        HashSet<ParameterExpression> requiredDefaults)
    {
        switch (statement)
        {
            case LoweredAssignmentStatement assignment:
                AnalyzeExpression(assignment.Operation, assigned, requiredDefaults);
                if (assignment.AssignedLocal != null)
                    assigned.Add(assignment.AssignedLocal);
                return assigned;

            case LoweredIfStatement conditional:
                AnalyzeExpression(conditional.Test, assigned, requiredDefaults);
                HashSet<ParameterExpression>? ifTrue = AnalyzeBlock(conditional.IfTrue, new HashSet<ParameterExpression>(assigned), requiredDefaults);
                HashSet<ParameterExpression>? ifFalse = conditional.IfFalse == null
                    ? new HashSet<ParameterExpression>(assigned)
                    : AnalyzeBlock(conditional.IfFalse, new HashSet<ParameterExpression>(assigned), requiredDefaults);
                return MergeBranches(ifTrue, ifFalse);

            case LoweredWhileStatement loop:
                AnalyzeExpression(loop.Test, assigned, requiredDefaults);
                AnalyzeBlock(loop.Body, new HashSet<ParameterExpression>(assigned), requiredDefaults);
                return assigned;

            case LoweredBlock block:
                return AnalyzeBlock(block, assigned, requiredDefaults);

            case LoweredReturnStatement returnStatement:
                if (returnStatement.Value != null)
                    AnalyzeExpression(returnStatement.Value, assigned, requiredDefaults);
                return null;

            case LoweredBreakStatement or LoweredContinueStatement:
                return null;

            default:
                throw new InvalidOperationException($"Unknown lowered statement '{statement.GetType().Name}'.");
        }
    }

    private static HashSet<ParameterExpression>? MergeBranches(HashSet<ParameterExpression>? ifTrue, HashSet<ParameterExpression>? ifFalse)
    {
        if (ifTrue == null)
            return ifFalse;
        if (ifFalse == null)
            return ifTrue;

        ifTrue.IntersectWith(ifFalse);
        return ifTrue;
    }

    [SuppressMessage("Correctness", "SS004:Implement Equals() and GetHashcode() methods for a type used in a collection.", Justification = "ParameterExpression instances are scoped symbols whose reference identity defines assignment.")]
    private static void AnalyzeExpression(
        ValidatedExpression expression,
        HashSet<ParameterExpression> assigned,
        HashSet<ParameterExpression> requiredDefaults)
    {
        foreach (ParameterExpression local in expression.LocalReads)
        {
            if (!assigned.Contains(local))
                requiredDefaults.Add(local);
        }
    }
}