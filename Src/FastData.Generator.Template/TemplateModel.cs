namespace Genbox.FastData.Generator.Template;

/// <summary>Provides type information shared by all templates.</summary>
public sealed class TemplateModel
{
    /// <summary>Gets or sets the lookup key type.</summary>
    public required Type KeyType { get; set; }

    /// <summary>Gets or sets the associated value type.</summary>
    public required Type ValueType { get; set; }
}