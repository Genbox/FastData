using System.Linq.Expressions;
using System.Reflection;

namespace Genbox.FastData.Generators.EarlyExits;

public class AllocationGatherTransform : IExprTransform
{
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
        public Dictionary<CallSignature, ParameterExpression> Variables { get; } = new Dictionary<CallSignature, ParameterExpression>(CallSignatureComparer.Instance);
    }

    private sealed class AllocationGatherVisitor(AllocationGatherState state) : ExpressionVisitor
    {
        public List<Expression> Assignments { get; } = new List<Expression>();

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(EarlyExitFunctions))
            {
                Expression? instance = node.Object == null ? null : Visit(node.Object);
                List<Expression> arguments = new List<Expression>(node.Arguments.Count);
                foreach (Expression argument in node.Arguments)
                    arguments.Add(Visit(argument));

                MethodCallExpression updatedCall = node.Update(instance, arguments);
                CallSignature signature = CallSignature.Create(updatedCall);

                if (!state.Variables.TryGetValue(signature, out ParameterExpression? variable))
                {
                    string name = ToCamelCase(updatedCall.Method.Name);
                    variable = Variable(updatedCall.Type, name);
                    state.Variables.Add(signature, variable);
                    Assignments.Add(Assign(variable, updatedCall));
                }

                return variable;
            }

            return base.VisitMethodCall(node);
        }

        private static string ToCamelCase(string name) => char.ToLowerInvariant(name[0]) + name.Substring(1);
    }

    private readonly struct CallSignature(MethodInfo method, ArgumentSignature[] arguments)
    {
        public MethodInfo Method { get; } = method;
        public ArgumentSignature[] Arguments { get; } = arguments;

        public static CallSignature Create(MethodCallExpression node)
        {
            ArgumentSignature[] args = new ArgumentSignature[node.Arguments.Count];
            for (int i = 0; i < node.Arguments.Count; i++)
                args[i] = ArgumentSignature.Create(node.Arguments[i]);

            return new CallSignature(node.Method, args);
        }
    }

    private readonly struct ArgumentSignature : IEquatable<ArgumentSignature>
    {
        private ArgumentSignature(ArgumentKind kind, Type type, object? value, string? name, string? text)
        {
            Kind = kind;
            Type = type;
            Value = value;
            Name = name;
            Text = text;
        }

        public ArgumentKind Kind { get; }
        public Type Type { get; }
        public object? Value { get; }
        public string? Name { get; }
        public string? Text { get; }

        public static ArgumentSignature Create(Expression expression)
        {
            if (expression is ConstantExpression constant)
                return new ArgumentSignature(ArgumentKind.Constant, constant.Type, constant.Value, null, null);

            if (expression is ParameterExpression parameter)
                return new ArgumentSignature(ArgumentKind.Parameter, parameter.Type, null, parameter.Name, null);

            return new ArgumentSignature(ArgumentKind.Other, expression.Type, null, null, expression.ToString());
        }

        public bool Equals(ArgumentSignature other)
        {
            if (Kind != other.Kind || Type != other.Type)
                return false;

            return Kind switch
            {
                ArgumentKind.Constant => Equals(Value, other.Value),
                ArgumentKind.Parameter => string.Equals(Name, other.Name, StringComparison.Ordinal),
                ArgumentKind.Other => string.Equals(Text, other.Text, StringComparison.Ordinal),
                _ => false
            };
        }

        public override bool Equals(object? obj) => obj is ArgumentSignature other && Equals(other);

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(Kind);
            hash.Add(Type);
            hash.Add(Value);
            hash.Add(Name, StringComparer.Ordinal);
            hash.Add(Text, StringComparer.Ordinal);
            return hash.ToHashCode();
        }
    }

    private sealed class CallSignatureComparer : IEqualityComparer<CallSignature>
    {
        public static readonly CallSignatureComparer Instance = new CallSignatureComparer();

        public bool Equals(CallSignature x, CallSignature y)
        {
            if (!Equals(x.Method, y.Method))
                return false;

            if (x.Arguments.Length != y.Arguments.Length)
                return false;

            for (int i = 0; i < x.Arguments.Length; i++)
            {
                if (!x.Arguments[i].Equals(y.Arguments[i]))
                    return false;
            }

            return true;
        }

        public int GetHashCode(CallSignature obj)
        {
            HashCode hash = new HashCode();
            hash.Add(obj.Method);
            foreach (ArgumentSignature arg in obj.Arguments)
                hash.Add(arg);

            return hash.ToHashCode();
        }
    }

    private enum ArgumentKind
    {
        Constant,
        Parameter,
        Other
    }
}