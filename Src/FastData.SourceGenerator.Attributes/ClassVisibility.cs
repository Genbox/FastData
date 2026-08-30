namespace Genbox.FastData.SourceGenerator.Attributes;

/// <summary>Specifies the visibility of the generated class.</summary>
public enum ClassVisibility : byte
{
    /// <summary>No visibility has been selected.</summary>
    Unknown = 0,

    /// <summary>The class will be internal.</summary>
    Internal,

    /// <summary>The class will be public.</summary>
    Public
}