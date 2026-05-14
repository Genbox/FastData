using JetBrains.Annotations;

namespace Genbox.FastData.SourceGenerator.Attributes;

/// <summary>Requests generation of a static key/value lookup from assembly-level constant keys and values.</summary>
/// <typeparam name="TKey">The key type used by the generated lookup.</typeparam>
/// <typeparam name="TValue">The value type returned for matching keys.</typeparam>
[UsedImplicitly(ImplicitUseTargetFlags.Members)]
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class FastDataKeyValueAttribute<TKey, TValue> : Attribute
{
    /// <summary>Initializes a new instance of the <see cref="FastDataKeyValueAttribute{TKey,TValue}" /> class.</summary>
    /// <param name="name">The generated type name.</param>
    /// <param name="keys">The keys included in the generated lookup.</param>
    /// <param name="value">The values associated with <paramref name="keys" />.</param>
    public FastDataKeyValueAttribute(string name, TKey[] keys, TValue[] value)
    {
        Name = name;
        Keys = keys;
        Value = value;
    }

    /// <summary>Gets the generated type name.</summary>
    public string Name { get; }

    /// <summary>Gets the keys included in the generated lookup.</summary>
    public TKey[] Keys { get; }

    /// <summary>Gets the values associated with <see cref="Keys" />.</summary>
    public TValue[] Value { get; }

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