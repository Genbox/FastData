using System.Text;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;
using static Genbox.FastData.Ir.Validation.IrValidation;

namespace Genbox.FastData.Ir.Validation;

/// <summary>Validates a program during construction, throwing at the first invalid node.</summary>
internal sealed class IrProgramValidation
{
    private static readonly UTF8Encoding _utf8 = new UTF8Encoding(false, true);
    private readonly IrProgram _program;

    private IrProgramValidation(IrProgram program)
    {
        _program = program;
    }

    public static void Validate(IrProgram program)
    {
        IrDiagnosticPath root = IrDiagnosticPath.Root();
        Require(Enum.IsDefined(program.TextEncoding), "Unsupported text encoding.", root + ".TextEncoding");
        IrProgramValidation validation = new IrProgramValidation(program);

        // Check all signatures before bodies so calls can reference functions declared later.
        validation.ValidateDeclarations(root);
        validation.ValidateBodies(root);
    }

    public static void ValidateLiteral(IrProgram program, IrLiteral literal) => new IrProgramValidation(program).ValidateLiteral(literal, IrDiagnosticPath.Root("Argument"));

    private static void RequireName(string? name, IrDiagnosticPath path) => Require(name != null, "A declaration name is required.", path);

    private void ValidateDeclarations(IrDiagnosticPath root)
    {
        for (int i = 0; i < _program.Records.Count; i++)
        {
            IrRecord record = _program.Records[i];
            IrDiagnosticPath path = root.Named("Records", record.Name, i);
            RequireName(record.Name, path);

            for (int j = 0; j < record.Fields.Count; j++)
            {
                IrField field = record.Fields[j];
                IrDiagnosticPath fieldPath = path.Named("Fields", field.Name, j);
                RequireName(field.Name, fieldPath);
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
                    RequireName(external.Symbol, path);
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
            RequireName(data.Name, path);
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
        RequireName(function.Name, path);
        ValidateSignature(function.Signature, path + ".Signature");

        for (int i = 0; i < function.Parameters.Count; i++)
        {
            IrVariable parameter = function.Parameters[i];
            RequireName(parameter.Name, path.Named("Parameters", parameter.Name, i));
        }

        for (int i = 0; i < function.Locals.Count; i++)
        {
            IrVariable local = function.Locals[i];
            IrDiagnosticPath localPath = path.Named("Locals", local.Name, function.Parameters.Count + i);
            RequireName(local.Name, localPath);
            ValidateType(local.Type, localPath + ".Type");
        }
    }

    private void ValidateType(IrType type, IrDiagnosticPath path)
    {
        if (type == null)
            throw Fail("A type is required.", path);
        if (type.Kind == IrTypeKind.Record)
            _program.Resolve(type.RecordId!.Value, path + ".RecordId");
        if (type.Kind == IrTypeKind.Array)
            ValidateType(type.ElementType!, path + ".ElementType");
    }

    private void ValidateLiteral(IrLiteral literal, IrDiagnosticPath path)
    {
        if (literal == null)
            throw Fail("A literal is required.", path);

        if (literal is IrCharLiteral character)
            Require(IrCharLiteral.IsValid(character.CharacterKind, character.Value), "Character value is outside its character kind.", path);

        ValidateType(literal.Type, path + ".Type");

        switch (literal)
        {
            case IrStringLiteral text when _program.TextEncoding == IrTextEncoding.Utf8:
                try
                {
                    _utf8.GetByteCount(text.Value);
                }
                catch (EncoderFallbackException exception)
                {
                    throw Fail($"String contains an unpaired surrogate: {exception.Message}", path);
                }

                break;
            case IrNullLiteral:
                Require(literal.Type.AcceptsNull, "The literal type does not admit null.", path, IrDiagnosticCode.TypeMismatch);
                break;
            case IrPackedArrayLiteral<byte> packed when packed.ElementType.CharacterKind == IrCharacterKind.Ascii:
                foreach (byte value in packed.AsSpan())
                    Require(value <= 0x7F, "Packed character value is outside ASCII.", path);
                break;
            case IrPackedStringArrayLiteral strings:
                ValidateStrings(strings, path);
                break;
            case IrArrayLiteral array:
                Require(array.ElementType.Kind is IrTypeKind.Array or IrTypeKind.Record, "Scalar and String arrays must use packed literals.", path);
                for (int i = 0; i < array.Elements.Count; i++)
                    ValidateElement(array.ElementType, array.Elements[i], path.Index("Elements", i), "Array literal element type does not match its array.");
                break;
            case IrRecordLiteral record:
            {
                IrRecord declaration = _program.Resolve(record.RecordId);
                Require(declaration.Fields.Count == record.Fields.Count, "Record literal field count does not match its declaration.", path);
                for (int i = 0; i < declaration.Fields.Count; i++)
                    ValidateElement(declaration.Fields[i].Type, record.Fields[i], path.Index("Fields", i), $"Record field '{declaration.Fields[i].Name}' has an incompatible literal.");
                break;
            }
        }
    }

    private void ValidateStrings(IrPackedStringArrayLiteral strings, IrDiagnosticPath path)
    {
        Require(strings.Encoding == _program.TextEncoding, "Packed strings must use the program's encoding.", path);
        Require(strings.Offsets.Count > 0 && strings.Offsets[0] == 0 && strings.Offsets[strings.Count] == strings.Data.Count, "Packed string offsets must cover the buffer.", path);
        Require(strings.NullBitmap.Count == 0 || strings.NullBitmap.Count == (strings.Count / 8) + (strings.Count % 8 == 0 ? 0 : 1), "Packed string null bitmap has the wrong length.", path);

        for (int i = 0; i < strings.Count; i++)
        {
            int start = strings.Offsets[i];
            int end = strings.Offsets[i + 1];
            Require(start <= end && end <= strings.Data.Count, "Packed string offsets must be ordered and in bounds.", path);
            Require(!strings.IsNull(i) || start == end, "Null strings must have empty slices.", path);

            if (strings.Encoding == IrTextEncoding.Utf16)
                Require((end & 1) == 0, "UTF-16 string boundaries must align to code units.", path);
            else
            {
                try
                {
                    // A valid combined buffer can still have an offset that splits a UTF-8 sequence.
                    strings.ValidateUtf8(i);
                }
                catch (DecoderFallbackException exception)
                {
                    throw Fail($"Packed string contains invalid UTF-8: {exception.Message}", path.Index("Strings", i));
                }
            }
        }
    }

    private void ValidateElement(IrType target, IrLiteral element, IrDiagnosticPath path, string mismatch)
    {
        // Validate character values, null types, and aggregate contents before comparing types.
        if (element is IrNullLiteral or IrCharLiteral or IrStringLiteral || element.Type.Kind is IrTypeKind.Array or IrTypeKind.Record)
            ValidateLiteral(element, path);

        Require(target == element.Type, mismatch, path, IrDiagnosticCode.TypeMismatch);
    }

    /// <summary>Validates a function body while tracking assignments on reachable continuing paths.</summary>
    private sealed class BodyValidation(IrProgramValidation owner, IrFunction function)
    {
        public void Run(IrDiagnosticPath path)
        {
            HashSet<IrVariableId> assigned = new HashSet<IrVariableId>();
            for (int i = 0; i < function.Parameters.Count; i++)
                assigned.Add(new IrVariableId(i));

            if (ValidateBlock(function.Body, assigned, path).ContinueAssignments != null)
                throw Fail($"Function '{function.Name}' can fall through without returning a value.", path, IrDiagnosticCode.MissingReturn);
        }

        private Flow ValidateBlock(IrBlock block, HashSet<IrVariableId>? assigned, IrDiagnosticPath path, bool canBreak = false)
        {
            if (block == null)
                throw Fail("A block is required.", path);

            Flow flow = new Flow { ContinueAssignments = assigned };

            // Unreachable statements still require valid bindings, types, and break placement.
            for (int i = 0; i < block.Statements.Count; i++)
            {
                Flow current = ValidateStatement(block.Statements[i], flow.ContinueAssignments, path.Index("Statements", i), canBreak);
                flow.ContinueAssignments = current.ContinueAssignments;
                flow.BreakAssignments = IntersectAssignments(flow.BreakAssignments, current.BreakAssignments);
            }

            return flow;
        }

        private Flow ValidateStatement(IrStatement statement, HashSet<IrVariableId>? assigned, IrDiagnosticPath path, bool canBreak)
        {
            switch (statement)
            {
                case null:
                    throw Fail("A statement is required.", path);
                case IrAssignmentStatement assignment:
                    RequireAssignable(ResolveVariable(assignment.VariableId, path + ".VariableId").Type, assignment.Value, assigned, path + ".Value");
                    assigned?.Add(assignment.VariableId);
                    return new Flow { ContinueAssignments = assigned };
                case IrIfStatement branch:
                {
                    RequireAssignable(IrType.Bool, branch.Condition, assigned, path + ".Condition");
                    Flow trueFlow = ValidateBlock(branch.IfTrue, Copy(assigned), path + ".IfTrue", canBreak);
                    Flow falseFlow = branch.IfFalse == null ? new Flow { ContinueAssignments = assigned } : ValidateBlock(branch.IfFalse, Copy(assigned), path + ".IfFalse", canBreak);
                    return new Flow { ContinueAssignments = IntersectAssignments(trueFlow.ContinueAssignments, falseFlow.ContinueAssignments), BreakAssignments = IntersectAssignments(trueFlow.BreakAssignments, falseFlow.BreakAssignments) };
                }
                case IrLoopStatement loop:
                {
                    RequireAssignable(IrType.Bool, loop.Condition, assigned, path + ".Condition");
                    Flow bodyFlow = ValidateBlock(loop.Body, Copy(assigned), path + ".Body", true);

                    // Only a literal true condition excludes the zero-iteration exit; nested breaks stop here.
                    return new Flow { ContinueAssignments = loop.Condition is IrBoolLiteral { Value: true } ? bodyFlow.BreakAssignments : IntersectAssignments(bodyFlow.BreakAssignments, assigned) };
                }
                case IrBreakStatement:
                    Require(canBreak, "Break requires a loop.", path);
                    return new Flow { BreakAssignments = assigned };
                case IrReturnStatement returned:
                    RequireAssignable(function.ReturnType, returned.Value, assigned, path + ".Value");
                    return new Flow();
                default:
                    throw Fail($"Unsupported IR statement '{statement.GetType().Name}'.", path);
            }
        }

        private void RequireAssignable(IrType target, IrExpression value, HashSet<IrVariableId>? assigned, IrDiagnosticPath path)
        {
            IrType valueType = ValidateExpression(value, assigned, path);
            Require(target == valueType, "Expression type is incompatible with its destination.", path, IrDiagnosticCode.TypeMismatch);
        }

        private IrType ValidateExpression(IrExpression expression, HashSet<IrVariableId>? assigned, IrDiagnosticPath path)
        {
            Stack<(IrExpression Expression, IrDiagnosticPath Path, int Stage)> pending = new Stack<(IrExpression, IrDiagnosticPath, int)>();
            Stack<IrType> types = new Stack<IrType>();
            pending.Push((expression, path, 0));

            while (pending.Count > 0)
            {
                (IrExpression current, IrDiagnosticPath currentPath, int stage) = pending.Pop();
                switch (current)
                {
                    case null:
                        throw Fail("An expression is required.", currentPath);
                    case IrLiteral literal:
                        owner.ValidateLiteral(literal, currentPath);
                        types.Push(literal.Type);
                        break;
                    case IrDataExpression data:
                        types.Push(owner._program.Resolve(data.DataId, currentPath + ".DataId").Type);
                        break;
                    case IrVariableExpression variable:
                    {
                        IrVariable declaration = ResolveVariable(variable.VariableId, currentPath + ".VariableId");
                        if (assigned?.Contains(variable.VariableId) == false)
                            throw Fail($"Variable '{declaration.Name}' is read before it is definitely assigned.", currentPath + ".VariableId", IrDiagnosticCode.UnassignedVariable);
                        types.Push(declaration.Type);
                        break;
                    }
                    case IrFieldExpression field:
                        if (stage == 0)
                            VisitChild(field.Receiver, currentPath + ".Receiver");
                        else
                        {
                            IrType receiver = types.Pop();
                            IrField declaration = owner._program.Resolve(field.FieldId, currentPath + ".FieldId");
                            Require(receiver.Kind == IrTypeKind.Record && receiver.RecordId == field.FieldId.RecordId, "Field reads require a receiver of the field's declaring record type.", currentPath + ".Receiver", IrDiagnosticCode.TypeMismatch);
                            types.Push(declaration.Type);
                        }
                        break;
                    case IrIndexExpression index:
                        if (stage == 0)
                            VisitChild(index.Collection, currentPath + ".Collection");
                        else if (stage == 1)
                        {
                            Require(types.Peek().Kind == IrTypeKind.Array, "Indexing requires an array.", currentPath + ".Collection", IrDiagnosticCode.TypeMismatch);
                            VisitChild(index.Index, currentPath + ".Index");
                        }
                        else
                        {
                            Require(types.Pop() == IrType.I32, "Array indices must be I32.", currentPath + ".Index", IrDiagnosticCode.TypeMismatch);
                            types.Push(types.Pop().ElementType!);
                        }
                        break;
                    case IrBinaryExpression binary:
                        if (stage == 0)
                            VisitChild(binary.Left, currentPath + ".Left");
                        else if (stage == 1)
                            VisitChild(binary.Right, currentPath + ".Right");
                        else
                        {
                            IrType right = types.Pop();
                            types.Push(IrOperationRules.Binary(binary, types.Pop(), right, currentPath));
                        }
                        break;
                    case IrLogicalExpression logical:
                        if (stage == 0)
                            VisitChild(logical.Left, currentPath + ".Left");
                        else if (stage == 1)
                            VisitChild(logical.Right, currentPath + ".Right");
                        else
                        {
                            IrType right = types.Pop();
                            types.Push(IrOperationRules.Logical(logical.Operation, types.Pop(), right, currentPath));
                        }
                        break;
                    case IrConversionExpression conversion:
                        if (stage == 0)
                        {
                            owner.ValidateType(conversion.TargetType, currentPath + ".TargetType");
                            VisitChild(conversion.Operand, currentPath + ".Operand");
                        }
                        else
                            types.Push(IrOperationRules.Conversion(conversion.Conversion, types.Pop(), conversion.TargetType, currentPath));
                        break;
                    case IrFunctionCallExpression call:
                    {
                        IrFunctionSignature signature = owner._program.ResolveCallable(call.FunctionId, currentPath + ".FunctionId").Signature;
                        if (stage == 0)
                            Require(signature.Parameters.Count == call.Arguments.Count, "Call argument count does not match its signature.", currentPath + ".Arguments", IrDiagnosticCode.TypeMismatch);
                        else
                            Require(signature.Parameters[stage - 1] == types.Pop(), "Expression type is incompatible with its destination.", currentPath.Index("Arguments", stage - 1), IrDiagnosticCode.TypeMismatch);

                        if (stage < call.Arguments.Count)
                            VisitChild(call.Arguments[stage], currentPath.Index("Arguments", stage));
                        else
                            types.Push(signature.ReturnType);
                        break;
                    }
                    default:
                        throw Fail($"Unsupported IR expression '{current.GetType().Name}'.", currentPath);
                }

                void VisitChild(IrExpression child, IrDiagnosticPath childPath)
                {
                    pending.Push((current, currentPath, stage + 1));
                    pending.Push((child, childPath, 0));
                }
            }

            return types.Pop();
        }

        private IrVariable ResolveVariable(IrVariableId id, IrDiagnosticPath path)
        {
            if (function.TryGetVariable(id, out IrVariable? variable))
                return variable;

            throw Fail($"No variable is declared with ID {id.Value}.", path, IrDiagnosticCode.MissingDeclaration);
        }

        private static HashSet<IrVariableId>? Copy(HashSet<IrVariableId>? assigned) => assigned == null ? null : new HashSet<IrVariableId>(assigned);

        // Null means unreachable. Intersect the left set in place; branch and loop entries are copied
        // to keep each exit's captured assignments independent.
        private static HashSet<IrVariableId>? IntersectAssignments(HashSet<IrVariableId>? left, HashSet<IrVariableId>? right)
        {
            if (left == null)
                return right;

            if (right != null)
                left.IntersectWith(right);

            return left;
        }

        // Null marks an unreachable exit; an empty set marks a reachable exit with zero definite assignments.
        private struct Flow
        {
            public HashSet<IrVariableId>? ContinueAssignments { get; set; }
            public HashSet<IrVariableId>? BreakAssignments { get; set; }
        }
    }
}