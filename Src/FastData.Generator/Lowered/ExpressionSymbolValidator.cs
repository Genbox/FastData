using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Genbox.FastData.Generator.Lowered;

internal sealed class ExpressionSymbolValidator(
    string compilerName,
    HashSet<ParameterExpression> declarations,
    Dictionary<string, ParameterExpression> externalSymbols,
    HashSet<string> externalDataNames) : ExpressionVisitor
{
    private readonly Dictionary<string, ParameterExpression> _activeSymbols = new Dictionary<string, ParameterExpression>(externalSymbols, StringComparer.Ordinal);

    internal static void ValidateLambda(LambdaExpression expression, string compilerName) => Validate(expression, compilerName, false);

    internal static void ValidateFragment(Expression expression, string compilerName) => Validate(expression, compilerName, true);

    private static void Validate(Expression expression, string compilerName, bool allowExternalSymbols)
    {
        ExpressionSymbolInventory inventory = new ExpressionSymbolInventory(compilerName);
        inventory.Visit(expression);
        Dictionary<string, ParameterExpression> externalSymbols = inventory.GetExternalSymbols(allowExternalSymbols);
        HashSet<string> externalDataNames = inventory.GetExternalDataNames();
        foreach (string name in externalSymbols.Keys)
        {
            if (externalDataNames.Contains(name))
                throw new NotSupportedException($"Expression symbol name '{name}' collides with captured external data and cannot be emitted safely by {compilerName}.");
        }

        new ExpressionSymbolValidator(compilerName, inventory.Declarations, externalSymbols, externalDataNames).Visit(expression);
    }

    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        VisitScope(node.Parameters, node.Body);
        return node;
    }

    protected override Expression VisitBlock(BlockExpression node)
    {
        EnterScope(node.Variables);
        try
        {
            foreach (Expression expression in node.Expressions)
                Visit(expression);
        }
        finally
        {
            ExitScope(node.Variables);
        }
        return node;
    }

    [SuppressMessage("Correctness", "SS004:Implement Equals() and GetHashcode() methods for a type used in a collection.", Justification = "ParameterExpression instances are scoped symbols whose reference identity distinguishes declarations.")]
    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (!declarations.Contains(node))
            return node;

        if (!_activeSymbols.TryGetValue(node.Name!, out ParameterExpression? existing))
            throw new NotSupportedException($"Local symbol '{node.Name}' is referenced outside its declaring scope and cannot be emitted safely by {compilerName}.");

        if (!ReferenceEquals(existing, node))
            throw new NotSupportedException($"Expression symbol name '{node.Name}' refers to multiple symbol identities and cannot be emitted safely by {compilerName}.");

        return node;
    }

    private void VisitScope(IReadOnlyList<ParameterExpression> symbols, Expression body)
    {
        EnterScope(symbols);
        try
        {
            Visit(body);
        }
        finally
        {
            ExitScope(symbols);
        }
    }

    private void EnterScope(IReadOnlyList<ParameterExpression> symbols)
    {
        foreach (ParameterExpression symbol in symbols)
        {
            if (externalDataNames.Contains(symbol.Name!))
                throw new NotSupportedException($"Expression symbol name '{symbol.Name}' collides with captured external data and cannot be emitted safely by {compilerName}.");
            if (_activeSymbols.TryGetValue(symbol.Name!, out ParameterExpression? existing))
            {
                if (!ReferenceEquals(existing, symbol))
                    throw new NotSupportedException($"Expression symbol name '{symbol.Name}' refers to multiple symbol identities and cannot be emitted safely by {compilerName}.");
                throw new NotSupportedException($"Expression symbol '{symbol.Name}' is declared by more than one active scope and cannot be emitted safely by {compilerName}.");
            }

            _activeSymbols.Add(symbol.Name!, symbol);
        }
    }

    private void ExitScope(IReadOnlyList<ParameterExpression> symbols)
    {
        foreach (ParameterExpression symbol in symbols)
            _activeSymbols.Remove(symbol.Name!);
    }

    private sealed class ExpressionSymbolInventory(string compilerName) : ExpressionVisitor
    {
        private readonly List<ParameterExpression> _references = new List<ParameterExpression>();
        private readonly List<CapturedBinding> _capturedBindings = new List<CapturedBinding>();

        internal HashSet<ParameterExpression> Declarations { get; } = new HashSet<ParameterExpression>();

        [SuppressMessage("Correctness", "SS004:Implement Equals() and GetHashcode() methods for a type used in a collection.", Justification = "ParameterExpression instances are scoped symbols whose reference identity distinguishes declarations.")]
        internal Dictionary<string, ParameterExpression> GetExternalSymbols(bool allowExternalSymbols)
        {
            Dictionary<string, ParameterExpression> symbols = new Dictionary<string, ParameterExpression>(StringComparer.Ordinal);
            foreach (ParameterExpression symbol in _references)
            {
                ValidateName(symbol, compilerName);
                if (Declarations.Contains(symbol))
                    continue;

                if (!allowExternalSymbols)
                    throw new NotSupportedException($"Lambda symbol '{symbol.Name}' is neither an input nor a declared local and cannot be emitted safely by {compilerName}.");

                if (symbols.TryGetValue(symbol.Name!, out ParameterExpression? existing) && !ReferenceEquals(existing, symbol))
                    throw new NotSupportedException($"Expression symbol name '{symbol.Name}' refers to multiple symbol identities and cannot be emitted safely by {compilerName}.");

                symbols[symbol.Name!] = symbol;
            }
            return symbols;
        }

        internal HashSet<string> GetExternalDataNames()
        {
            Dictionary<string, CapturedBinding> bindings = new Dictionary<string, CapturedBinding>(StringComparer.Ordinal);
            foreach (CapturedBinding binding in _capturedBindings)
            {
                string name = binding.Member.Name;
                if (bindings.TryGetValue(name, out CapturedBinding existing) && !existing.Matches(binding))
                    throw new NotSupportedException($"Captured external-data name '{name}' refers to multiple bindings and cannot be emitted safely by {compilerName}.");

                bindings[name] = binding;
            }
            return new HashSet<string>(bindings.Keys, StringComparer.Ordinal);
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            foreach (ParameterExpression parameter in node.Parameters)
                AddDeclaration(parameter);
            return base.VisitLambda(node);
        }

        protected override Expression VisitBlock(BlockExpression node)
        {
            foreach (ParameterExpression variable in node.Variables)
                AddDeclaration(variable);
            return base.VisitBlock(node);
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            _references.Add(node);
            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression is not ConstantExpression constant)
                return base.VisitMember(node);

            _capturedBindings.Add(new CapturedBinding(node.Member, constant.Value));
            return node;
        }

        [SuppressMessage("Correctness", "SS004:Implement Equals() and GetHashcode() methods for a type used in a collection.", Justification = "ParameterExpression instances are scoped symbols whose reference identity distinguishes declarations.")]
        private void AddDeclaration(ParameterExpression symbol)
        {
            ValidateName(symbol, compilerName);
            if (!Declarations.Add(symbol))
                throw new NotSupportedException($"Expression symbol '{symbol.Name}' is declared by more than one scope and cannot be emitted safely by {compilerName}.");
        }

        private static void ValidateName(ParameterExpression symbol, string compilerName)
        {
            if (string.IsNullOrEmpty(symbol.Name))
                throw new NotSupportedException($"Unnamed expression symbols are not supported by {compilerName}.");
        }

        private readonly record struct CapturedBinding(MemberInfo Member, object? Instance)
        {
            internal bool Matches(CapturedBinding other) => Member.Equals(other.Member) && ReferenceEquals(Instance, other.Instance);
        }
    }
}