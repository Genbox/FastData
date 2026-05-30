namespace Genbox.FastData.Config;

public class StructureSettings
{
    private readonly Dictionary<string, object> _settings = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, object> _defaultSettings = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    internal void AddDefault(string key, object value) => _defaultSettings.Add(key, value);

    public void SetSetting(string key, object value) => _settings.Add(key, value);

    public T GetSetting<T>(string key)
    {
        if (!_settings.TryGetValue(key, out object? value))
            return (T)_defaultSettings[key];

        return (T)value;
    }
}