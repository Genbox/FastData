# Expression Compilation

FastData uses `System.Linq.Expressions` as a convenient construction format for generated hash functions and early exits. The shared generator lowers structural trees into a small, closed intermediate representation. Validated leaf values retain their source expression for target-specific rendering.

## Compilation Boundary

`ExpressionCompiler` exposes three explicit root contracts:

- `GetLambdaBody(LambdaExpression)` renders a lambda as a complete program. Lowering validates results against the lambda return type, declares its locals, preserves its statements, and emits an explicit `return`.
- `GetStatements(BlockExpression)` renders a raw block as a statement fragment. Control-flow statements such as early exits must be wrapped in a block before they are inserted into an existing generated method.
- `GetValue(Expression)` validates and renders one value fragment, such as an index, helper call, or arithmetic expression. Values are inline fragments and therefore have no indentation parameter.

Templates must not infer a lambda's result local or append a second return. A lambda owns its complete body and result contract.
Lambda output omits its final line terminator so templates can interpolate it directly before their closing brace.

## Lowered Representation

The internal lowered representation models structure rather than duplicating the expression tree:

- Programs contain one body block and the local `ParameterExpression` instances that require default initialization.
- Blocks contain scoped `ParameterExpression` local declarations followed by statements. A `LoweredBlock` is itself a `LoweredStatement`, so nested lexical scopes need no separate wrapper node.
- Statements are blocks, assignments, `if`, `while`, `break`, `continue`, and `return`.
- Every value position contains one `ValidatedExpression`: its original source expression plus the local `ParameterExpression` identities it reads.

There is no raw-code escape node, discarded value statement, assignment-as-value, or implicit block result. Unsupported shapes fail with a clear generation-time diagnostic during validation, lowering, or target-default selection instead of being returned as malformed syntax. Early-exit fragments use explicit return statements and retained `DefaultExpression` values rather than parameter names containing target keywords.

Captured member accesses are recognized as external data during validation, while free `ParameterExpression` references in statement blocks are validated as variables supplied by the surrounding template. `StringHashInfo` requires each captured array to match an `AdditionalData` entry by name, element type, and array identity. Captured-data names are reserved so inputs, locals, and free fragment variables cannot shadow them. The lambda-bearing `StringHashInfo` contract stores `Expression<TDelegate>` so its delegate shape is checked where the tree enters the generation pipeline; `GetLambdaBody` accepts the common `LambdaExpression` base for rendering.

`ExpressionSymbolValidator` is the sole owner of symbol names, declaration identity, lexical scope, external-name collisions, and the closed-lambda invariant. Once that pass succeeds, `ExpressionProgramLowerer` only tracks which locals are active so it can classify local reads and assignment targets; it does not repeat symbol or scope validation. Value-shape validation and local-read collection remain in the lowerer. Target compilers render the retained source expression only after it has passed those checks. This keeps target-specific behavior such as C# unchecked arithmetic, C++ casts and string operations, and Rust wrapping arithmetic in the language backends without allowing those backends to reinterpret structural control flow.

## Result Normalization

Lowering normalizes the terminal expression of every non-void lambda:

- A terminal value becomes `return value`.
- A terminal local read becomes `return local`.
- A terminal assignment remains an assignment and is followed by `return target`. This matters for Rust, where assignment evaluates to unit rather than the assigned value.
- A terminal variable-free block is flattened while preserving order.
- A terminal block with locals remains a nested scope containing its return.

This removes the former convention where templates knew that hash lambdas ended in `hash`.

## Validation And Scoping

The IR uses each local's `ParameterExpression` object directly as its identity. Names must be non-empty and unique within each active lexical scope because target source identifies variables by name. Distinct locals may reuse a name in non-overlapping sibling scopes, but inputs and external template variables cannot be shadowed. One local identity cannot be declared by multiple blocks, and a local cannot be referenced outside its declaring block.

Lambdas are closed: every parameter reference must be a declared lambda input or an active local. Raw statement blocks may reference named template variables because their surrounding generated method supplies those declarations. Assignments target `ParameterExpression` variables and currently support set, add, subtract, and exclusive-or forms.

Lowering preserves every assignment as a semantic statement, and definite-assignment analysis examines that complete statement sequence. The renderer declares locals in block order, then renders every lowered statement explicitly in its original order.

A pure, single-pass definite-assignment analysis follows lowering and returns the set of locals read before an explicit write. Portable primitive locals in that set receive a target-rendered default initializer; locals proven to be written first keep an uninitialized declaration. A required default for a reference, enum, nullable, or other non-portable type is rejected until its semantics are modeled per target. Branches merge their fallthrough assignment sets by intersection, terminating paths do not contribute to the merge, and loops conservatively allow zero iterations.

The supported loop shape is the expression-tree form of a `while`: a loop containing a conditional body whose false branch breaks the loop. Value-producing loops, value-returning conditionals, enum constants, user-defined or lifted operators, compound-assignment conversions, method calls with `ref` or `out` parameters, and open expression-tree node kinds are rejected until shared validation and every backend define their cross-target semantics. Features that change statement or control-flow structure also require an explicit lowered statement form.

## Extending Expression Support

When adding an expression feature:

1. Add symbol invariants to `ExpressionSymbolValidator`, value validation and local-read collection to `ExpressionProgramLowerer`, or a closed statement shape when the feature changes control flow.
2. Add shared structural rendering only for syntax common to every target.
3. Override retained-expression rendering in a language compiler when overflow, casts, indexing, or calls differ.
4. Add core lowering tests and non-Docker rendering contract tests for C#, C++, and Rust.
5. Exercise real generator snapshots and target compilation where the environment supports it.

Do not add a fallback that calls `Expression.ToString()` or copies unvalidated source text into output.