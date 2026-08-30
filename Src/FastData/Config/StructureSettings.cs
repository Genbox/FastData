namespace Genbox.FastData.Config;

/// <summary>Stores advanced, named settings used to tune generated data structures.</summary>
public class StructureSettings
{
    private readonly Dictionary<string, object> _defaultSettings = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, object> _settings = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    internal void AddDefault(string key, object value) => _defaultSettings.Add(key, value);

    /// <summary>Sets an advanced structure setting, overriding its configured default.</summary>
    /// <param name="key">The setting key, typically one of the values in <see cref="KnownSettings" />.</param>
    /// <param name="value">The setting value.</param>
    public void SetSetting(string key, object value) => _settings[key] = value;

    /// <summary>Gets an advanced structure setting or its configured default.</summary>
    /// <typeparam name="T">The expected setting value type.</typeparam>
    /// <param name="key">The setting key, typically one of the values in <see cref="KnownSettings" />.</param>
    /// <returns>The configured setting value.</returns>
    public T GetSetting<T>(string key)
    {
        if (!_settings.TryGetValue(key, out object? value))
            return (T)_defaultSettings[key];

        return (T)value;
    }
}