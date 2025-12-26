using JetBrains.Annotations;

namespace Genbox.FastData.SourceGenerator.Attributes;

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class FastDataAttribute<TKey> : Attribute
{
    public FastDataAttribute(string name, TKey[] keys)
    {
        Name = name;
        Keys = keys;
    }

    public string Name { get; }
    public TKey[] Keys { get; }
    public StructureType StructureType { get; set; }
    public bool IgnoreCase { get; set; }
    public string? Namespace { get; set; }
    public ClassVisibility ClassVisibility { get; set; }
    public ClassType ClassType { get; set; }
    public AnalysisLevel AnalysisLevel { get; set; }
}