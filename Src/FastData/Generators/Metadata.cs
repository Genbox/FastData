using System.Globalization;
using JetBrains.Annotations;

namespace Genbox.FastData.Generators;

[UsedImplicitly(ImplicitUseTargetFlags.Members, Reason = "Used by code generators when in Release mode")]
public sealed class Metadata(Version version, DateTimeOffset timestamp)
{
    public string Program { get; } = "FastData " + version;
    public string Timestamp { get; } = timestamp.ToString("yyyy-MM-dd HH:mm:ss", DateTimeFormatInfo.InvariantInfo) + " UTC";
}