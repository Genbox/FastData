namespace Genbox.FastData.Generator.Template;

/// <summary>Common model for all templates</summary>
public sealed class TemplateModel
{
    public required Type KeyType { get; set; }
    public required Type ValueType { get; set; }
}