using System.Text;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Ir.Validation;

/// <summary>Validates a program during construction, throwing at the first invalid node.</summary>
internal sealed class IrProgramValidation(IrProgram program)
{
    private readonly IrProgram _program = program;

    public static void Validate(IrProgram program)
    {
        IrDiagnosticPath root = IrDiagnosticPath.Root();
        IrProgramValidation validation = new IrProgramValidation(program);

        // Check all signatures before bodies so calls can reference functions declared later.
        validation.ValidateDeclarations(root);
        validation.ValidateBodies(root);
    }

    public static void ValidateLiteral(IrProgram program, IrLiteral literal) => new IrProgramValidation(program).ValidateLiteral(literal, IrDiagnosticPath.Root("Argument"));

    private void ValidateDeclarations(IrDiagnosticPath root)
    {
        for (int i = 0; i < _program.Records.Count; i++)
        {
            IrRecord record = _program.Records[i];
            IrDiagnosticPath path = root.Named("Records", record.Name, i);

            for (int j = 0; j < record.Fields.Count; j++)
            {
                IrField field = record.Fields[j];
                IrDiagnosticPath fieldPath = path.Named("Fields", field.Name, j);
                ValidateType(field.Type, fieldPath + ".Type");
            }
        }

        for (int i = 0; i < _program.Callables.Count; i++)
        {
            switch (_program.Callables[i])
            {
                case IrFunction function:
                    ValidateFunction(function, root.Named("Callables", function.Name, i));
                    break;
                case IrExternalFunction external:
                {
                    IrDiagnosticPath path = root.Named("Callables", external.Symbol, i);
                    ValidateSignature(external.Signature, path + ".Signature");
                    break;
                }
                default:
                    throw Fail("Unsupported callable declaration.", root.Index("Callables", i));
            }
        }

        for (int i = 0; i < _program.Data.Count; i++)
        {
            IrData data = _program.Data[i];
            IrDiagnosticPath path = root.Named("Data", data.Name, i);
            ValidateLiteral(data.Value, path + ".Value");
        }
    }

    private void ValidateBodies(IrDiagnosticPath root)
    {
        for (int i = 0; i < _program.Callables.Count; i++)
        {
            if (_program.Callables[i] is IrFunction function)
                new BodyValidation(this, function).Run(root.Named("Callables", function.Name, i) + ".Body");
        }
    }

    private void ValidateSignature(IrFunctionSignature signature, IrDiagnosticPath path)
    {
        ValidateType(signature.ReturnType, path + ".ReturnType");

        for (int i = 0; i < signature.Parameters.Count; i++)
            ValidateType(signature.Parameters[i], path.Index("Parameters", i));
    }

    private void ValidateFunction(IrFunction function, IrDiagnosticPath path)
    {
        ValidateSignature(function.Signature, path + ".Signature");

        for (int i = 0; i < function.Locals.Count; i++)
        {
            IrVariable local = function.Locals[i];
            IrDiagnosticPath localPath = path.Named("Locals", local.Name, function.Parameters.Count + i);
            ValidateType(local.Type, localPath + ".Type");
        }
    }

    private void ValidateType(IrType type, IrDiagnosticPath path)
    {
        while (type.Kind == IrTypeKind.Array)
        {
            type = type.ElementType!;
            path += ".ElementType";
        }

        if (type.Kind == IrTypeKind.Record)
            _program.Resolve(type.RecordId!.Value, path + ".RecordId");
    }

    private void ValidateLiteral(IrLiteral literal, IrDiagnosticPath path)
    {
        if (literal == null)
            throw Fail("A literal is required.", path);

        ValidateType(literal.Type, path + ".Type");

        switch (literal)
        {
            case IrStringLiteral text when _program.TextEncoding == IrTextEncoding.Utf8:
                try
                {
                    _program.Text.GetLength(text.Value);
                }
                catch (EncoderFallbackException exception)
                {
                    throw Fail($"String contains an unpaired surrogate: {exception.Message}", path);
                }

                break;
            case IrPackedStringArrayLiteral strings:
                Require(strings.Encoding == _program.TextEncoding, "Packed strings must use the program's encoding.", path);
                break;
            case IrArrayLiteral array:
                for (int i = 0; i < array.Elements.Count; i++)
                    ValidateLiteral(array.Elements[i], path.Index("Elements", i));
                break;
            case IrRecordLiteral record:
            {
                IrRecord declaration = _program.Resolve(record.RecordId);
                Require(declaration.Fields.Count == record.Fields.Count, "Record literal field count does not match its declaration.", path);

                for (int i = 0; i < declaration.Fields.Count; i++)
                {
                    IrDiagnosticPath fieldPath = path.Index("Fields", i);
                    ValidateLiteral(record.Fields[i], fieldPath);
                    Require(declaration.Fields[i].Type == record.Fields[i].Type, $"Record field '{declaration.Fields[i].Name}' has an incompatible literal.", fieldPath, IrDiagnosticCode.TypeMismatch);
                }

                break;
            }
        }
    }

    /// <summary>Validates a function body while tracking assignments on reachable continuing paths.</summary>
    private sealed class BodyValidation(IrProgramValidation owner, IrFunction function)
    {
        public void Run(IrDiagnosticPath path)
        {
            HashSet<int> assigned = new HashSet<int>();
            for (int i = 0; i < function.Parameters.Count; i++)
                assigned.Add(i);

            if (ValidateBlock(function.Body, assigned, path).ContinueAssignments != null)
                throw Fail($"Function '{function.Name}' can fall through without returning a value.", path, IrDiagnosticCode.MissingReturn);
        }

        private Flow ValidateBlock(IrBlock block, HashSet<int>? assigned, IrDiagnosticPath path, bool canBreak = false)
        {
            if (block == null)
                throw Fail("A block is required.", path);

            HashSet<int>? continuing = assigned;
            HashSet<int>? breaking = null;

            // Unreachable statements still require valid bindings, types, and break placement.
            for (int i = 0; i < block.Statements.Count; i++)
            {
                Flow current = ValidateStatement(block.Statements[i], continuing, path.Index("Statements", i), canBreak);
                continuing = current.ContinueAssignments;
                breaking = IntersectAssignments(breaking, current.BreakAssignments);
            }

            return new Flow(continuing, breaking);
        }

        private Flow ValidateStatement(IrStatement statement, HashSet<int>? assigned, IrDiagnosticPath path, bool canBreak)
        {
            switch (statement)
            {
                case null:
                    throw Fail("A statement is required.", path);
                case IrAssignmentStatement assignment:
                    ResolveVariable(assignment.VariableId, path + ".VariableId");
                    ValidateExpression(assignment.Value, assigned, path + ".Value");
                    assigned?.Add(assignment.VariableId.Value);
                    return new Flow(assigned);
                case IrIfStatement branch:
                {
                    ValidateExpression(branch.Condition, assigned, path + ".Condition");
                    Flow trueFlow = ValidateBlock(branch.IfTrue, Copy(assigned), path + ".IfTrue", canBreak);
                    Flow falseFlow = branch.IfFalse == null ? new Flow(assigned) : ValidateBlock(branch.IfFalse, Copy(assigned), path + ".IfFalse", canBreak);
                    return new Flow(IntersectAssignments(trueFlow.ContinueAssignments, falseFlow.ContinueAssignments), IntersectAssignments(trueFlow.BreakAssignments, falseFlow.BreakAssignments));
                }
                case IrLoopStatement loop:
                {
                    ValidateExpression(loop.Condition, assigned, path + ".Condition");
                    Flow bodyFlow = ValidateBlock(loop.Body, Copy(assigned), path + ".Body", true);

                    // Only a literal true condition excludes the zero-iteration exit; nested breaks stop here.
                    return new Flow(loop.Condition is IrBoolLiteral { Value: true } ? bodyFlow.BreakAssignments : IntersectAssignments(bodyFlow.BreakAssignments, assigned));
                }
                case IrBreakStatement:
                    Require(canBreak, "Break requires a loop.", path);
                    return new Flow(BreakAssignments: assigned);
                case IrReturnStatement returned:
                    ValidateExpression(returned.Value, assigned, path + ".Value");
                    Require(function.ReturnType == returned.Value.Type, "Expression type is incompatible with its destination.", path + ".Value", IrDiagnosticCode.TypeMismatch);
                    return new Flow();
                default:
                    throw Fail($"Unsupported IR statement '{statement.GetType().Name}'.", path);
            }
        }

        private void ValidateExpression(IrExpression expression, HashSet<int>? assigned, IrDiagnosticPath path)
        {
            Stack<(IrExpression Expression, IrDiagnosticPath Path)> pending = new Stack<(IrExpression, IrDiagnosticPath)>();
            VisitChild(expression, path);

            while (pending.Count > 0)
            {
                (IrExpression current, IrDiagnosticPath currentPath) = pending.Pop();

                switch (current)
                {
                    case IrLiteral literal:
                        owner.ValidateLiteral(literal, currentPath);
                        break;
                    case IrDataExpression data:
                        Require(data.Type == owner._program.Resolve(data.DataId, currentPath + ".DataId").Type, "Data reference type does not match its declaration.", currentPath, IrDiagnosticCode.TypeMismatch);
                        break;
                    case IrVariableExpression variable:
                    {
                        IrVariable declaration = ResolveVariable(variable.VariableId, currentPath + ".VariableId");
                        if (assigned?.Contains(variable.VariableId.Value) == false)
                            throw Fail($"Variable '{declaration.Name}' is read before it is definitely assigned.", currentPath + ".VariableId", IrDiagnosticCode.UnassignedVariable);
                        break;
                    }
                    case IrFieldExpression field:
                    {
                        VisitChild(field.Receiver, currentPath + ".Receiver");
                        IrField declaration = owner._program.Resolve(field.FieldId, currentPath + ".FieldId");
                        Require(field.Type == declaration.Type, "Field reference type does not match its declaration.", currentPath, IrDiagnosticCode.TypeMismatch);
                        break;
                    }
                    case IrIndexExpression index:
                        VisitChild(index.Index, currentPath + ".Index");
                        VisitChild(index.Collection, currentPath + ".Collection");
                        break;
                    case IrBinaryExpression binary:
                        VisitChild(binary.Right, currentPath + ".Right");
                        VisitChild(binary.Left, currentPath + ".Left");
                        break;
                    case IrLogicalExpression logical:
                        VisitChild(logical.Right, currentPath + ".Right");
                        VisitChild(logical.Left, currentPath + ".Left");
                        break;
                    case IrConversionExpression conversion:
                        VisitChild(conversion.Operand, currentPath + ".Operand");
                        break;
                    case IrFunctionCallExpression call:
                    {
                        IrFunctionSignature signature = owner._program.ResolveCallable(call.FunctionId, currentPath + ".FunctionId").Signature;
                        Require(call.Type == signature.ReturnType, "Call result type does not match its declaration.", currentPath, IrDiagnosticCode.TypeMismatch);
                        Require(signature.Parameters.Count == call.Arguments.Count, "Call argument count does not match its signature.", currentPath + ".Arguments", IrDiagnosticCode.TypeMismatch);

                        for (int i = call.Arguments.Count - 1; i >= 0; i--)
                        {
                            IrDiagnosticPath argumentPath = currentPath.Index("Arguments", i);
                            VisitChild(call.Arguments[i], argumentPath);
                            Require(signature.Parameters[i] == call.Arguments[i].Type, "Expression type is incompatible with its destination.", argumentPath, IrDiagnosticCode.TypeMismatch);
                        }

                        break;
                    }
                    default:
                        throw Fail($"Unsupported IR expression '{current.GetType().Name}'.", currentPath);
                }
            }

            void VisitChild(IrExpression child, IrDiagnosticPath childPath)
            {
                if (child == null)
                    throw Fail("An expression is required.", childPath);
                Require(child.Type != null, "An expression result type is required.", childPath, IrDiagnosticCode.TypeMismatch);
                pending.Push((child, childPath));
            }
        }

        private IrVariable ResolveVariable(IrVariableId id, IrDiagnosticPath path)
        {
            if (!function.TryGetVariable(id, out IrVariable? variable))
                throw Fail($"No variable is declared with ID {id.Value}.", path, IrDiagnosticCode.MissingDeclaration);

            Require(id.Type == variable.Type, "Variable reference type does not match its declaration.", path, IrDiagnosticCode.TypeMismatch);
            return variable;
        }

        private static HashSet<int>? Copy(HashSet<int>? assigned) => assigned == null ? null : new HashSet<int>(assigned);

        // Null means unreachable. Intersect the left set in place; branch and loop entries are copied
        // to keep each exit's captured assignments independent.
        private static HashSet<int>? IntersectAssignments(HashSet<int>? left, HashSet<int>? right)
        {
            if (left == null)
                return right;

            if (right != null)
                left.IntersectWith(right);

            return left;
        }

        // Null marks an unreachable exit; an empty set marks a reachable exit with zero definite assignments.
        private readonly record struct Flow(HashSet<int>? ContinueAssignments = null, HashSet<int>? BreakAssignments = null);
    }
}