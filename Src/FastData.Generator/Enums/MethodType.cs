using JetBrains.Annotations;

namespace Genbox.FastData.Generator.Enums;

[PublicAPI]
public enum MethodType : byte
{
    Unknown = 0,
    Contains,
    TryLookup
}