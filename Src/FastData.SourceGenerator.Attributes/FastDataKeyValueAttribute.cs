using JetBrains.Annotations;

namespace Genbox.FastData.SourceGenerator.Attributes;

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class FastDataKeyValueAttribute<TKey, TValue> : Attribute
{
    public FastDataKeyValueAttribute(string name, TKey[] keys, TValue[] value)
    {
        Name = name;
        Keys = keys;
        Value = value;
    }

    public string Name { get; }
    public TKey[] Keys { get; }
    public TValue[] Value { get; }
    public StructureType StructureType { get; set; }
    public string? Namespace { get; set; }
    public ClassVisibility ClassVisibility { get; set; }
    public ClassType ClassType { get; set; }
    public AnalysisLevel AnalysisLevel { get; set; }
}