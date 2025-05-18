using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Genbox.FastData.InternalShared;

public class BuilderCodeVisitor : ExpressionVisitor
{
    private readonly StringBuilder _sb = new StringBuilder();

    public string GetCode(Expression expr)
    {
        Visit(expr);
        return _sb.ToString();
    }

    private static string GetTypeName(Type type)
    {
        if (type == typeof(void)) return "void";
        if (type == typeof(int)) return "int";
        if (type == typeof(string)) return "string";
        if (type == typeof(bool)) return "bool";
        if (type == typeof(byte)) return "byte";
        if (type == typeof(char)) return "char";
        if (type == typeof(decimal)) return "decimal";
        if (type == typeof(double)) return "double";
        if (type == typeof(float)) return "float";
        if (type == typeof(long)) return "long";
        if (type == typeof(object)) return "object";
        if (type == typeof(short)) return "short";
        if (type == typeof(sbyte)) return "sbyte";
        if (type == typeof(uint)) return "uint";
        if (type == typeof(ulong)) return "ulong";
        if (type == typeof(ushort)) return "ushort";
        return type.FullName.Replace('+', '.');
    }


    protected override Expression VisitConstant(ConstantExpression node)
    {
        if (node.Value == null)
            _sb.Append("Expression.Constant(null)");
        else if (node.Type == typeof(string))
            _sb.Append($"Expression.Constant(\"{node.Value}\")");
        else if (node.Type.IsPrimitive)
            _sb.Append($"Expression.Constant({node.Value})");
        else
            _sb.Append($"Expression.Constant(({node.Type.FullName}){node.Value})");
        return node;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        _sb.Append($"Expression.Parameter(typeof({node.Type.FullName}), \"{node.Name}\")");
        return node;
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        string op = node.NodeType.ToString();
        _sb.Append($"Expression.{op}(");
        Visit(node.Left);
        _sb.Append(", ");
        Visit(node.Right);
        if (node.Method != null)
            _sb.Append($", typeof({node.Method.DeclaringType.FullName}).GetMethod(\"{node.Method.Name}\")");
        _sb.Append(")");
        return node;
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        string op = node.NodeType.ToString();
        _sb.Append($"Expression.{op}(");
        Visit(node.Operand);
        if (node.Type != node.Operand.Type)
            _sb.Append($", typeof({node.Type.FullName})");
        _sb.Append(")");
        return node;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        _sb.Append("Expression.Call(");
        if (node.Object != null)
        {
            Visit(node.Object);
            _sb.Append(", ");
        }
        _sb.Append($"typeof({node.Method.DeclaringType.FullName}).GetMethod(\"{node.Method.Name}\")");
        if (node.Arguments.Count > 0)
        {
            _sb.Append(", ");
            for (int i = 0; i < node.Arguments.Count; i++)
            {
                Visit(node.Arguments[i]);
                if (i < node.Arguments.Count - 1) _sb.Append(", ");
            }
        }
        _sb.Append(")");
        return node;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        _sb.Append("Expression.MakeMemberAccess(");
        if (node.Expression != null)
        {
            Visit(node.Expression);
            _sb.Append(", ");
        }
        var m = node.Member;
        if (m is PropertyInfo pi)
            _sb.Append($"typeof({pi.DeclaringType.FullName}).GetProperty(\"{pi.Name}\")");
        else if (m is FieldInfo fi)
            _sb.Append($"typeof({fi.DeclaringType.FullName}).GetField(\"{fi.Name}\")");
        _sb.Append(")");
        return node;
    }

    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        _sb.Append($"Expression.Lambda<{typeof(T).FullName}>(");
        Visit(node.Body);
        if (node.Parameters.Count > 0)
        {
            _sb.Append(", ");
            for (int i = 0; i < node.Parameters.Count; i++)
            {
                Visit(node.Parameters[i]);
                if (i < node.Parameters.Count - 1) _sb.Append(", ");
            }
        }
        _sb.Append(")");
        return node;
    }

    protected override Expression VisitNew(NewExpression node)
    {
        _sb.Append($"Expression.New(typeof({node.Constructor.DeclaringType.FullName}).GetConstructor(new[] {{ ");
        for (int i = 0; i < node.Constructor.GetParameters().Length; i++)
        {
            _sb.Append($"typeof({node.Constructor.GetParameters()[i].ParameterType.FullName})");
            if (i < node.Constructor.GetParameters().Length - 1) _sb.Append(", ");
        }
        _sb.Append(" }})");
        if (node.Arguments.Count > 0)
        {
            _sb.Append(", ");
            for (int i = 0; i < node.Arguments.Count; i++)
            {
                Visit(node.Arguments[i]);
                if (i < node.Arguments.Count - 1) _sb.Append(", ");
            }
        }
        _sb.Append(")");
        return node;
    }

    protected override Expression VisitNewArray(NewArrayExpression node)
    {
        string fn = node.NodeType == ExpressionType.NewArrayInit ? "NewArrayInit" : "NewArrayBounds";
        _sb.Append($"Expression.{fn}(typeof({node.Type.GetElementType().FullName}), ");
        if (node.Expressions.Count > 0)
        {
            _sb.Append("new Expression[] { ");
            for (int i = 0; i < node.Expressions.Count; i++)
            {
                Visit(node.Expressions[i]);
                if (i < node.Expressions.Count - 1) _sb.Append(", ");
            }
            _sb.Append(" }");
        }
        _sb.Append(")");
        return node;
    }

    protected override Expression VisitListInit(ListInitExpression node)
    {
        _sb.Append("Expression.ListInit(");
        Visit(node.NewExpression);
        for (int i = 0; i < node.Initializers.Count; i++)
        {
            var init = node.Initializers[i];
            _sb.Append(", Expression.ElementInit(");
            _sb.Append($"typeof({init.AddMethod.DeclaringType.FullName}).GetMethod(\"{init.AddMethod.Name}\")");
            if (init.Arguments.Count > 0)
            {
                _sb.Append(", new Expression[] { ");
                for (int j = 0; j < init.Arguments.Count; j++)
                {
                    Visit(init.Arguments[j]);
                    if (j < init.Arguments.Count - 1) _sb.Append(", ");
                }
                _sb.Append(" }");
            }
            _sb.Append(")");
        }
        _sb.Append(")");
        return node;
    }

    protected override MemberBinding VisitMemberBinding(MemberBinding node)
    {
        switch (node.BindingType)
        {
            case MemberBindingType.Assignment:
                var assign = (MemberAssignment)node;
                _sb.Append("Expression.Bind(");
                _sb.Append($"typeof({node.Member.DeclaringType.FullName}).GetMember(\"{node.Member.Name}\")[0], ");
                Visit(assign.Expression);
                _sb.Append(")");
                break;
            case MemberBindingType.ListBinding:
                var list = (MemberListBinding)node;
                _sb.Append("Expression.ListBind(");
                _sb.Append($"typeof({node.Member.DeclaringType.FullName}).GetMember(\"{node.Member.Name}\")[0]");
                foreach (var init in list.Initializers)
                {
                    _sb.Append(", Expression.ElementInit(");
                    _sb.Append($"typeof({init.AddMethod.DeclaringType.FullName}).GetMethod(\"{init.AddMethod.Name}\")");
                    if (init.Arguments.Count > 0)
                    {
                        _sb.Append(", new Expression[] { ");
                        for (int j = 0; j < init.Arguments.Count; j++)
                        {
                            Visit(init.Arguments[j]);
                            if (j < init.Arguments.Count - 1) _sb.Append(", ");
                        }
                        _sb.Append(" }");
                    }
                    _sb.Append(")");
                }
                _sb.Append(")");
                break;
            case MemberBindingType.MemberBinding:
                var mb = (MemberMemberBinding)node;
                _sb.Append("Expression.MemberBind(");
                _sb.Append($"typeof({node.Member.DeclaringType.FullName}).GetMember(\"{node.Member.Name}\")[0]");
                foreach (var b in mb.Bindings)
                {
                    _sb.Append(", ");
                    VisitMemberBinding(b);
                }
                _sb.Append(")");
                break;
        }
        return node;
    }

    protected override Expression VisitMemberInit(MemberInitExpression node)
    {
        _sb.Append("Expression.MemberInit(");
        Visit(node.NewExpression);
        foreach (var b in node.Bindings)
        {
            _sb.Append(", ");
            VisitMemberBinding(b);
        }
        _sb.Append(")");
        return node;
    }

    protected override Expression VisitConditional(ConditionalExpression node)
    {
        _sb.Append("Expression.Condition(");
        Visit(node.Test);
        _sb.Append(", ");
        Visit(node.IfTrue);
        _sb.Append(", ");
        Visit(node.IfFalse);
        _sb.Append(")");
        return node;
    }

    protected override Expression VisitInvocation(InvocationExpression node)
    {
        _sb.Append("Expression.Invoke(");
        Visit(node.Expression);
        if (node.Arguments.Count > 0)
        {
            _sb.Append(", ");
            for (int i = 0; i < node.Arguments.Count; i++)
            {
                Visit(node.Arguments[i]);
                if (i < node.Arguments.Count - 1) _sb.Append(", ");
            }
        }
        _sb.Append(")");
        return node;
    }

    protected override Expression VisitTypeBinary(TypeBinaryExpression node)
    {
        _sb.Append("Expression.TypeIs(");
        Visit(node.Expression);
        _sb.Append($", typeof({node.TypeOperand.FullName}))");
        return node;
    }

    protected override Expression VisitBlock(BlockExpression node)
    {
        _sb.Append("Expression.Block(");
        if (node.Variables.Count > 0)
        {
            _sb.Append("new[] { ");
            for (int i = 0; i < node.Variables.Count; i++)
            {
                Visit(node.Variables[i]);
                if (i < node.Variables.Count - 1) _sb.Append(", ");
            }
            _sb.Append(" }, ");
        }
        for (int i = 0; i < node.Expressions.Count; i++)
        {
            Visit(node.Expressions[i]);
            if (i < node.Expressions.Count - 1) _sb.Append(", ");
        }
        _sb.Append(")");
        return node;
    }

    protected override Expression VisitSwitch(SwitchExpression node)
    {
        _sb.Append("Expression.Switch(");
        Visit(node.SwitchValue);
        _sb.Append(", ");
        if (node.DefaultBody != null)
            Visit(node.DefaultBody);
        else
            _sb.Append("null");

        if (node.Comparison != null)
        {
            _sb.Append(", ");
            _sb.Append($"typeof({GetTypeName(node.Comparison.DeclaringType)}).GetMethod(\"{node.Comparison.Name}\")");
        }

        foreach (var c in node.Cases)
        {
            _sb.Append(", Expression.SwitchCase(");
            Visit(c.Body);
            _sb.Append(", new Expression[] { ");
            for (int i = 0; i < c.TestValues.Count; i++)
            {
                Visit(c.TestValues[i]);
                if (i < c.TestValues.Count - 1) _sb.Append(", ");
            }
            _sb.Append(" })");
        }

        _sb.Append(")");
        return node;
    }

    protected override CatchBlock VisitCatchBlock(CatchBlock node)
    {
        _sb.Append("Expression.Catch(");
        if (node.Variable != null)
            Visit(node.Variable);
        else
            _sb.Append("null");
        _sb.Append(", ");
        Visit(node.Body);
        if (node.Filter != null)
        {
            _sb.Append(", ");
            Visit(node.Filter);
        }
        _sb.Append(")");
        return node;
    }

    protected override Expression VisitTry(TryExpression node)
    {
        _sb.Append("Expression.TryCatchFinally(");
        Visit(node.Body);
        _sb.Append(", new[] { ");
        for (int i = 0; i < node.Handlers.Count; i++)
        {
            VisitCatchBlock(node.Handlers[i]);
            if (i < node.Handlers.Count - 1) _sb.Append(", ");
        }
        _sb.Append(" }");
        if (node.Finally != null)
        {
            _sb.Append(", ");
            Visit(node.Finally);
        }
        if (node.Fault != null)
        {
            _sb.Append(", ");
            Visit(node.Fault);
        }
        _sb.Append(")");
        return node;
    }

    protected override Expression VisitIndex(IndexExpression node)
    {
        if (node.Indexer == null)
        {
            _sb.Append("Expression.ArrayIndex(");
            Visit(node.Object);
            _sb.Append(", ");
            Visit(node.Arguments[0]);
            _sb.Append(")");
        }
        else
        {
            _sb.Append("Expression.MakeIndex(");
            Visit(node.Object);
            _sb.Append(", ");
            _sb.Append($"typeof({node.Indexer.DeclaringType.FullName}).GetProperty(\"{node.Indexer.Name}\")");
            _sb.Append(", new Expression[] { ");
            for (int i = 0; i < node.Arguments.Count; i++)
            {
                Visit(node.Arguments[i]);
                if (i < node.Arguments.Count - 1) _sb.Append(", ");
            }
            _sb.Append(" })");
        }
        return node;
    }

    protected override Expression VisitDefault(DefaultExpression node)
    {
        _sb.Append($"Expression.Default(typeof({GetTypeName(node.Type)}))");
        return node;
    }

    protected override Expression VisitRuntimeVariables(RuntimeVariablesExpression node)
    {
        _sb.Append("Expression.RuntimeVariables(");
        for (int i = 0; i < node.Variables.Count; i++)
        {
            Visit(node.Variables[i]);
            if (i < node.Variables.Count - 1) _sb.Append(", ");
        }
        _sb.Append(")");
        return node;
    }

    protected override Expression VisitLabel(LabelExpression node)
    {
        _sb.Append("Expression.Label(");
        _sb.Append($"typeof({GetTypeName(node.Target.Type)}), \"{node.Target.Name}\")");
        if (node.DefaultValue != null)
        {
            _sb.Append(", ");
            Visit(node.DefaultValue);
        }
        _sb.Append(")");
        return node;
    }

    protected override Expression VisitGoto(GotoExpression node)
    {
        _sb.Append($"Expression.{node.Kind}(");
        _sb.Append($"Expression.Label(typeof({GetTypeName(node.Target.Type)}), \"{node.Target.Name}\")");
        if (node.Value != null)
        {
            _sb.Append(", ");
            Visit(node.Value);
        }
        _sb.Append(")");
        return node;
    }

    protected override Expression VisitLoop(LoopExpression node)
    {
        _sb.Append("Expression.Loop(");
        Visit(node.Body);
        if (node.BreakLabel != null || node.ContinueLabel != null)
        {
            _sb.Append(", ");
            _sb.Append($"Expression.Label(typeof({node.BreakLabel?.Type.FullName}), \"{node.BreakLabel?.Name}\")");
            _sb.Append(", ");
            _sb.Append($"Expression.Label(typeof({node.ContinueLabel?.Type.FullName}), \"{node.ContinueLabel?.Name}\")");
        }
        _sb.Append(")");
        return node;
    }
}