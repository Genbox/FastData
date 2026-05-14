using JetBrains.Annotations;

namespace Genbox.FastData.SourceGenerator.Attributes;

/// <summary>Requests generation of a static membership lookup from assembly-level constant keys.</summary>
/// <typeparam name="TKey">The key type used by the generated lookup.</typeparam>
[UsedImplicitly(ImplicitUseTargetFlags.Members)]
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class FastDataAttribute<TKey> : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="FastDataAttribute{TKey}" /> class.</summary>
    /// <param name="name">The generated type name.</param>
    /// <param name="keys">The keys included in the generated membership lookup.</param>
    public FastDataAttribute(string name, TKey[] keys)
    {
        Name = name;
        Keys = keys;
    }

    /// <summary>Gets the generated type name.</summary>
    public string Name { get; }

    /// <summary>Gets the keys included in the generated membership lookup.</summary>
    public TKey[] Keys { get; }

    /// <summary>Gets or sets the generated structure type. The default value lets FastData choose automatically.</summary>
    public StructureType StructureType { get; set; }

    /// <summary>Gets or sets a value indicating whether string keys use ordinal-ignore-case comparison.</summary>
    public bool IgnoreCase { get; set; }

    /// <summary>Gets or sets the namespace for the generated C# type.</summary>
    public string? Namespace { get; set; }

    /// <summary>Gets or sets the visibility of the generated C# type.</summary>
    public ClassVisibility ClassVisibility { get; set; }

    /// <summary>Gets or sets whether the generated C# type is static, an instance class, or a struct.</summary>
    public ClassType ClassType { get; set; }

    /// <summary>Gets or sets the amount of string-hash analysis performed during source generation.</summary>
    public AnalysisLevel AnalysisLevel { get; set; }
}