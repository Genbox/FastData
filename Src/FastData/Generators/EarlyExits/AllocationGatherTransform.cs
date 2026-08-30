using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Expressions;

namespace Genbox.FastData.Generators.EarlyExits;

/// <summary>Reuses repeated generator helper calls by assigning their results to local variables on first use.</summary>
public class AllocationGatherTransform : IExprTransform
{
    /*
        If we just print out each expression, we will get something like this:

        public bool Contains(string key)
        {
            if (Length(key) < 3 || Length(key) > 6)
                return false;

            if (UnitAt(key, 0) != 'Æ')
                return false;

            if (UnitAt(key, 0) < 'A')
                return false;
        }

        That's suboptimal for performance due to repeated calls. However, if we just detect the calls and print them out in the beginning, it is
        still not good, as the allocations will happen before they are needed.

        public bool Contains(string key)
        {
            uint len = Length(key);
            uint unitAt = UnitAt(key, 0);

            if (len < 3 || len > 6)
                return false;

            if (unitAt != 'Æ')
                return false;

            if (unitAt < 'A')
                return false;
        }

        However, by adding the gatherer transform, it will register the allocation the first time they are needed, and it now looks like this:

        public bool Contains(string key)
        {
            uint len = Length(key);

            if (len < 3 || len > 6)
                return false;

            uint unitAt = UnitAt(key, 0);

            if (unitAt != 'Æ')
                return false;

            if (unitAt < 'A')
                return false;
        }
    */

    /// <inheritdoc />
    public object CreateState() => new AllocationGatherState();

    /// <inheritdoc />
    public void Transform(AnnotatedExpr expr, object state, ICollection<AnnotatedExpr> output)
    {
        if (state is not AllocationGatherState gatherState)
            throw new ArgumentException("State must have been created by this transform.", nameof(state));
        if (output == null)
            throw new ArgumentNullException(nameof(output));

        AllocationGatherVisitor visitor = gatherState.Visitor;
        visitor.Reset();
        Expression updated;

        if (expr.Kind == ExprKind.Assignment &&
            expr.Expression is BinaryExpression { NodeType: ExpressionType.Assign, Left: ParameterExpression variable, Right: MethodCallExpression call } &&
            AllocationGatherVisitor.IsGatherable(call))
            updated = visitor.RegisterAllocation(variable, call);
        else
            updated = visitor.Visit(expr.Expression) ?? expr.Expression;

        foreach (Expression assignment in visitor.Assignments)
            output.Add(new AnnotatedExpr(assignment, ExprKind.Assignment));

        output.Add(new AnnotatedExpr(updated, expr.Kind));
    }

    private sealed class AllocationGatherState
    {
        private readonly HashSet<string> _variableNames = new HashSet<string>(StringComparer.Ordinal);

        internal AllocationGatherState() => Visitor = new AllocationGatherVisitor(this);

        public Dictionary<MethodCallSignature, GatheredAllocation> Variables { get; } = new Dictionary<MethodCallSignature, GatheredAllocation>();
        internal AllocationGatherVisitor Visitor { get; }

        internal void Add(MethodCallSignature signature, ParameterExpression variable, MethodCallExpression call) =>
            Variables.Add(signature, GatheredAllocation.Create(variable, call));

        internal string CreateVariableName(string preferredName)
        {
            if (_variableNames.Add(preferredName))
                return preferredName;

            int suffix = 2;
            while (!_variableNames.Add(preferredName + suffix))
                suffix++;

            return preferredName + suffix;
        }

        internal void Invalidate(ParameterExpression variable)
        {
            List<MethodCallSignature> stale = new List<MethodCallSignature>();

            foreach (KeyValuePair<MethodCallSignature, GatheredAllocation> pair in Variables)
            {
                if (ReferenceEquals(pair.Value.Variable, variable) || pair.Value.DependsOn(variable))
                    stale.Add(pair.Key);
            }

            foreach (MethodCallSignature signature in stale)
                Variables.Remove(signature);
        }

        internal void ReserveName(ParameterExpression variable)
        {
            if (!string.IsNullOrEmpty(variable.Name))
                _variableNames.Add(variable.Name);
        }
    }

    private sealed class GatheredAllocation(ParameterExpression variable, ParameterExpression[] dependencies)
    {
        public ParameterExpression Variable { get; } = variable;

        public static GatheredAllocation Create(ParameterExpression variable, MethodCallExpression call)
        {
            ParameterDependencyVisitor visitor = new ParameterDependencyVisitor();
            visitor.Visit(call);
            return new GatheredAllocation(variable, visitor.Dependencies.ToArray());
        }

        public bool DependsOn(ParameterExpression variable)
        {
            foreach (ParameterExpression dependency in dependencies)
            {
                if (ReferenceEquals(dependency, variable))
                    return true;
            }

            return false;
        }
    }

    private sealed class ParameterDependencyVisitor : ExpressionVisitor
    {
        public List<ParameterExpression> Dependencies { get; } = new List<ParameterExpression>();

        protected override Expression VisitParameter(ParameterExpression node)
        {
            foreach (ParameterExpression dependency in Dependencies)
            {
                if (ReferenceEquals(dependency, node))
                    return node;
            }

            Dependencies.Add(node);
            return node;
        }
    }

    private sealed class AllocationGatherVisitor(AllocationGatherState state) : ExpressionVisitor
    {
        public List<Expression> Assignments { get; } = new List<Expression>();

        internal void Reset() => Assignments.Clear();

        internal BinaryExpression RegisterAllocation(ParameterExpression variable, MethodCallExpression call)
        {
            state.ReserveName(variable);
            Expression? instance = call.Object == null ? null : Visit(call.Object);
            ReadOnlyCollection<Expression> arguments = Visit(call.Arguments);
            MethodCallExpression updatedCall = call.Update(instance, arguments);
            MethodCallSignature signature = MethodCallSignature.Create(updatedCall);
            state.Variables.TryGetValue(signature, out GatheredAllocation? existing);

            // The right-hand side observes the pre-assignment value. Invalidate only after visiting it, then retain a
            // canonical result when the call does not itself depend on the symbol being overwritten.
            state.Invalidate(variable);

            if (existing != null)
            {
                if (!existing.DependsOn(variable) && !state.Variables.ContainsKey(signature))
                    state.Add(signature, variable, updatedCall);

                return Assign(variable, existing.Variable);
            }

            GatheredAllocation allocation = GatheredAllocation.Create(variable, updatedCall);
            if (!allocation.DependsOn(variable))
                state.Variables.Add(signature, allocation);
            return Assign(variable, updatedCall);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (IsGatherable(node))
            {
                Expression? instance = node.Object == null ? null : Visit(node.Object);
                ReadOnlyCollection<Expression> arguments = Visit(node.Arguments);
                MethodCallExpression updatedCall = node.Update(instance, arguments);
                MethodCallSignature signature = MethodCallSignature.Create(updatedCall);

                if (!state.Variables.TryGetValue(signature, out GatheredAllocation? allocation))
                {
                    string name = state.CreateVariableName(BuildVariableName(updatedCall));
                    ParameterExpression variable = Variable(updatedCall.Type, name);
                    state.Add(signature, variable, updatedCall);
                    Assignments.Add(Assign(variable, updatedCall));
                    return variable;
                }

                return allocation.Variable;
            }

            return base.VisitMethodCall(node);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (IsAssignment(node.NodeType) && node.Left is ParameterExpression assignedVariable)
                state.ReserveName(assignedVariable);

            Expression updated = base.VisitBinary(node);

            if (IsAssignment(node.NodeType) && node.Left is ParameterExpression variable)
                state.Invalidate(variable);

            return updated;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            state.ReserveName(node);
            return node;
        }

        internal static bool IsGatherable(MethodCallExpression node) =>
            node.Method.DeclaringType == typeof(GeneratorFunctions) && node.Type != typeof(bool);

        private static bool IsAssignment(ExpressionType nodeType) => nodeType is
            ExpressionType.Assign or
            ExpressionType.AddAssign or
            ExpressionType.AddAssignChecked or
            ExpressionType.AndAssign or
            ExpressionType.DivideAssign or
            ExpressionType.ExclusiveOrAssign or
            ExpressionType.LeftShiftAssign or
            ExpressionType.ModuloAssign or
            ExpressionType.MultiplyAssign or
            ExpressionType.MultiplyAssignChecked or
            ExpressionType.OrAssign or
            ExpressionType.PowerAssign or
            ExpressionType.RightShiftAssign or
            ExpressionType.SubtractAssign or
            ExpressionType.SubtractAssignChecked;

        [SuppressMessage("Minor Code Smell", "S1643:Strings should not be concatenated using \'+\' in a loop")]
        private static string BuildVariableName(MethodCallExpression call)
        {
            string baseName = char.ToLowerInvariant(call.Method.Name[0]) + call.Method.Name.Substring(1);

            // Append constant argument values to disambiguate calls to the same method with different constant arguments
            foreach (Expression arg in call.Arguments)
            {
                if (arg is ConstantExpression constant && constant.Value != null)
                {
                    string value = constant.Value.ToString()!;
                    baseName += value.Length > 0 && value[0] == '-' ? "Neg" + value.TrimStart('-') : value;
                }
            }

            return baseName;
        }
    }
}