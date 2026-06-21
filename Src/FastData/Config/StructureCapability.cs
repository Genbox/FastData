namespace Genbox.FastData.Config;

/// <summary>Specifies generated data-structure capabilities required by the caller.</summary>
[Flags]
public enum StructureCapability
{
    /// <summary>No additional capabilities are required.</summary>
    None = 0,

    /// <summary>The generated data structure must support membership checks.</summary>
    Membership = 1,

    /// <summary>The generated data structure must support key/value lookups.</summary>
    KeyValueLookup = 2,

    /// <summary>The generated data structure must support lazily enumerating stored keys and values.</summary>
    Enumeration = 4,

    /// <summary>The generated data structure must support direct contiguous access to stored keys and values.</summary>
    DirectAccess = 8
}