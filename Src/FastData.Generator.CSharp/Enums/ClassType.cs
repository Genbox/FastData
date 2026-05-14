namespace Genbox.FastData.Generator.CSharp.Enums;

/// <summary>Specifies the shape of the generated C# type.</summary>
public enum ClassType
{
    /// <summary>No type shape was specified.</summary>
    Unknown = 0,

    /// <summary>Generate a static class with static members.</summary>
    Static,

    /// <summary>Generate an instance class with instance members.</summary>
    Instance,

    /// <summary>Generate a struct with instance members.</summary>
    Struct
}