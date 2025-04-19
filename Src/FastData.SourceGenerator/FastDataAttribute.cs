using Genbox.FastData.Enums;
using Genbox.FastData.Generator.CSharp.Enums;

namespace Genbox.FastData.SourceGenerator;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class FastDataAttribute<T> : Attribute
{
    public FastDataAttribute(string name, T[] data)
    {
        Name = name;
        Data = data;
    }

    public string Name { get; }
    public T[] Data { get; }
    public StructureType StructureType { get; set; }
    public string? Namespace { get; set; }
    public ClassVisibility ClassVisibility { get; set; }
    public ClassType ClassType { get; set; }
    public StorageOption StorageOptions { get; set; }
}