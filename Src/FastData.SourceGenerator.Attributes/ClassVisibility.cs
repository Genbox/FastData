namespace Genbox.FastData.SourceGenerator.Attributes;

/// <summary>Specifies the visibility of the generated class.</summary>
public enum ClassVisibility : byte
{
    Unknown = 0,
    /// <summary>The class will be internal.</summary>
    Internal,
    /// <summary>The class will be public.</summary>
    Public
}