using System.Diagnostics.CodeAnalysis;

namespace Genbox.FastData.Generator.Template.Abstracts;

/// <summary>Identifies data models that can be passed to generated templates.</summary>
[SuppressMessage("Design", "CA1040:Avoid empty interfaces", Justification = "This established public marker interface restricts template data models without imposing behavior.")]
public interface ITemplateData;