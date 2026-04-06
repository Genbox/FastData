using JetBrains.Annotations;

namespace Genbox.FastData.Generator.Enums;

[PublicAPI]
public enum BranchType : byte
{
    Unknown = 0,
    Switch,
    If
}