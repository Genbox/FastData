using JetBrains.Annotations;

namespace Genbox.FastData.Generator.Enums;

/// <summary>Identifies the lookup operation emitted by a generator.</summary>
[PublicAPI]
public enum MethodType : byte
{
    /// <summary>The lookup operation has not been selected.</summary>
    Unknown = 0,

    /// <summary>Tests whether a key is present.</summary>
    Contains,

    /// <summary>Attempts to retrieve the value associated with a key.</summary>
    TryLookup
}