namespace Genbox.FastData.Generator.CSharp.Enums;

/// <summary>Specifies the visibility of the generated C# type.</summary>
public enum ClassVisibility : byte
{
    /// <summary>No visibility was specified.</summary>
    Unknown = 0,

    /// <summary>Generate an internal type.</summary>
    Internal,

    /// <summary>Generate a public type.</summary>
    Public
}