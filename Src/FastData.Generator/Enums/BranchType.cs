using JetBrains.Annotations;

namespace Genbox.FastData.Generator.Enums;

/// <summary>Specifies how branch-based generated code should be emitted.</summary>
[PublicAPI]
public enum BranchType : byte
{
    /// <summary>Emit a switch expression or statement when the target template supports it.</summary>
    Switch,

    /// <summary>Emit if/else branches.</summary>
    If
}