using System.Globalization;

namespace Genbox.FastData.Generators;

public sealed class Metadata(Version version, DateTimeOffset timestamp)
{
    public string Program { get; } = "FastData " + version;
    public string Timestamp { get; } = timestamp.ToString("yyyy-MM-dd HH:mm:ss", DateTimeFormatInfo.InvariantInfo) + " UTC";
}