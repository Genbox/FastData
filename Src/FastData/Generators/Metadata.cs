using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using JetBrains.Annotations;

namespace Genbox.FastData.Generators;

/// <summary>Describes the FastData version and generation time embedded in generated output.</summary>
/// <param name="version">The FastData assembly version.</param>
/// <param name="timestamp">The generation timestamp.</param>
[UsedImplicitly(ImplicitUseTargetFlags.Members, Reason = "Used by code generators when in Release mode")]
[SuppressMessage("Naming", "CA1724:Type names should not match namespaces", Justification = "Metadata is the established public model name used by generator templates.")]
public sealed class Metadata(Version version, DateTimeOffset timestamp)
{
    /// <summary>Gets the generator product and version text.</summary>
    public string Program { get; } = "FastData " + version;

    /// <summary>Gets the UTC generation timestamp text.</summary>
    public string Timestamp { get; } = timestamp.ToString("yyyy-MM-dd HH:mm:ss", DateTimeFormatInfo.InvariantInfo) + " UTC";
}