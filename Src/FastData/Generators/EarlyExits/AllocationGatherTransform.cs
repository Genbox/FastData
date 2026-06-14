using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Expressions;

namespace Genbox.FastData.Generators.EarlyExits;

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

    public object CreateState() => new AllocationGatherState();

    public IEnumerable<AnnotatedExpr> Transform(AnnotatedExpr expr, object state)
    {
        AllocationGatherVisitor visitor = new AllocationGatherVisitor((AllocationGatherState)state);
        Expression updated = visitor.Visit(expr.Expression) ?? expr.Expression;

        foreach (Expression assignment in visitor.Assignments)
            yield return new AnnotatedExpr(assignment, ExprKind.Assignment);

        yield return new AnnotatedExpr(updated, expr.Kind);
    }

    private sealed class AllocationGatherState
    {
        public Dictionary<MethodCallSignature, ParameterExpression> Variables { get; } = new Dictionary<MethodCallSignature, ParameterExpression>();
    }

    private sealed class AllocationGatherVisitor(AllocationGatherState state) : ExpressionVisitor
    {
        public List<Expression> Assignments { get; } = new List<Expression>();

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(GeneratorFunctions) && node.Type != typeof(bool))
            {
                Expression? instance = node.Object == null ? null : Visit(node.Object);
                List<Expression> arguments = new List<Expression>(node.Arguments.Count);
                foreach (Expression argument in node.Arguments)
                    arguments.Add(Visit(argument));

                MethodCallExpression updatedCall = node.Update(instance, arguments);
                MethodCallSignature signature = MethodCallSignature.Create(updatedCall);

                if (!state.Variables.TryGetValue(signature, out ParameterExpression? variable))
                {
                    string name = BuildVariableName(updatedCall);
                    variable = Variable(updatedCall.Type, name);
                    state.Variables.Add(signature, variable);
                    Assignments.Add(Assign(variable, updatedCall));
                }

                return variable;
            }

            return base.VisitMethodCall(node);
        }

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
                    baseName += value.StartsWith("-", StringComparison.Ordinal) ? "Neg" + value.Substring(1) : value;
                }
            }

            return baseName;
        }
    }
}