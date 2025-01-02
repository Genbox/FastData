// ReSharper disable All

using Genbox.FastData.Enums;

namespace Genbox.FastData;

[global::System.AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class FastDataAttribute<T> : global::System.Attribute
{
    public FastDataAttribute(string name, T[] data)
    {
        Name = name;
        Data = data;
    }

    public string Name { get; }
    public T[] Data { get; }
    public string? Namespace { get; set; }
    public ClassVisibility ClassVisibility { get; set; }
    public ClassType ClassType { get; set; }

#if INTERNAL_BUILD
    public StorageMode StorageMode { get; set; }
#else
    internal StorageMode StorageMode { get; set; }
#endif
}