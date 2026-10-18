using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Genbox.FastData.Generator.Ir.Building;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Expressions;
using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Generator.Ir.Lowering;

[SuppressMessage("Correctness", "SS004:Implement Equals() and GetHashcode() methods for a type used in a collection.", Justification = "ParameterExpression instances are scoped symbols whose reference identity is the variable identity.")]
internal sealed class FrontendExpressions(GeneratorConfigBase config, FastDataProgramBuilder builder, Type keyClrType, IrType keyType)
{
    private readonly ExpressionTranslator.SharedData _shared = new ExpressionTranslator.SharedData(builder.Program, builder.Keys.FoldFunctionId);

    // Early exits reject keys before the structure runs: allocations become locals and each exit returns a miss.
    public List<IrStatement> EarlyExits()
    {
        Dictionary<ParameterExpression, IrVariableId> variables = new Dictionary<ParameterExpression, IrVariableId>();
        foreach (ParameterExpression key in KeyParameterFinder.Find(config.EarlyExits.Select(static x => x.Expression), keyClrType))
            variables.Add(key, builder.KeyParameterId);

        ExpressionTranslator translator = new ExpressionTranslator(builder, builder.FindLocal, variables, _shared);
        List<IrStatement> statements = new List<IrStatement>();

        foreach (AnnotatedExpr expr in config.EarlyExits)
        {
            if (expr.Kind == ExprKind.EarlyExit)
                statements.Add(If(translator.Value(expr.Expression), Return(I32(-1))));
            else if (expr.Expression is BinaryExpression assignment)
                statements.Add(translator.Assignment(assignment));
            else
                throw new NotSupportedException($"Early-exit expression '{expr.Expression.NodeType}' is not supported by the IR lowering.");
        }

        return statements;
    }

    // The hash must equal the frontend's: numeric keys use the shared numeric hash, strings the selected hash expression.
    public IrFunctionCallExpression Hash()
    {
        if (keyType != IrType.String)
            return Call(builder.Keys.AddNumericHashHelper(), Read(builder.KeyParameterId));

        if (config is not StringGeneratorConfig { HashInfo: {} info })
            throw new InvalidOperationException("String hash tables require a hash expression.");

        IrFunctionBuilder function = builder.Program.Function("Hash", IrType.U64);
        LambdaExpression lambda = info.Expression;
        IrVariableId data = function.Parameter("data", IrType.String);
        IrVariableId length = function.Parameter("length", IrType.I32);
        Dictionary<ParameterExpression, IrVariableId> variables = new Dictionary<ParameterExpression, IrVariableId>
        {
            [lambda.Parameters[0]] = data,
            [lambda.Parameters[1]] = length
        };

        function.Define(new ExpressionTranslator(builder, function.Local, variables, _shared).TranslateLambda(lambda));

        // Frontend hashes consume bytes, while text lengths count the selected code units.
        IrExpression byteLength = builder.Text.Length(Read(builder.KeyParameterId));
        if (builder.Program.Text.Encoding == IrTextEncoding.Utf16)
            byteLength = Multiply(byteLength, I32(2));
        return Call(function.Id, Read(builder.KeyParameterId), byteLength);
    }

    /// <summary>Finds the frontend's "key" parameter, which early exits reference by instance rather than through a lambda.</summary>
    private sealed class KeyParameterFinder(Type keyType) : ExpressionVisitor
    {
        private readonly HashSet<ParameterExpression> _found = new HashSet<ParameterExpression>();

        public static HashSet<ParameterExpression> Find(IEnumerable<Expression> expressions, Type keyType)
        {
            KeyParameterFinder finder = new KeyParameterFinder(keyType);
            foreach (Expression expression in expressions)
                finder.Visit(expression);
            return finder._found;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (node.Name == "key" && node.Type == keyType)
                _found.Add(node);
            return node;
        }
    }
}