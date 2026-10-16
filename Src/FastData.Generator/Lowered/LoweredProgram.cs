using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Genbox.FastData.Generator.Lowered;

/// <summary>A structured expression program with local-initialization requirements.</summary>
public sealed class LoweredProgram(LoweredBlock body, ISet<ParameterExpression> requiredDefaults)
{
    private readonly ISet<ParameterExpression> _requiredDefaults = requiredDefaults;

    /// <summary>Gets the statement body.</summary>
    public LoweredBlock Body { get; } = body;

    /// <summary>Reports whether a local must be initialized before the program executes.</summary>
    [SuppressMessage("Correctness", "SS004:Implement Equals() and GetHashcode() methods for a type used in a collection.", Justification = "ParameterExpression instances are scoped symbols whose reference identity defines initialization requirements.")]
    public bool RequiresDefaultInitialization(ParameterExpression local) => _requiredDefaults.Contains(local);
}