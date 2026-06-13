using Microsoft.Extensions.Configuration;

namespace Genbox.FastData.BenchmarkHarness.Runner.Configuration;

internal static class ConfigurationLoader
{
    public static Settings Load(FileInfo? configFile)
    {
        Settings settings = new Settings();

        if (configFile != null)
            new ConfigurationBuilder().AddJsonFile(configFile.FullName, false, false).Build().GetSection("benchmark").Bind(settings);

        return settings;
    }
}