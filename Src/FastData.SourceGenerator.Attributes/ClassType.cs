namespace Genbox.FastData.SourceGenerator.Attributes;

/// <summary>Specifies the type of class to generate.</summary>
public enum ClassType
{
    Unknown = 0,

    /// <summary>It will generate a static class with static methods and properties.</summary>
    Static,

    /// <summary>It will generate a class with instance methods and properties.</summary>
    Instance,

    /// <summary>It will generate a struct with instance methods and properties.</summary>
    Struct
}