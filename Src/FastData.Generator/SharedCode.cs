// ReSharper disable CanSimplifyDictionaryLookupWithTryAdd

using Genbox.FastData.Generator.Enums;

namespace Genbox.FastData.Generator;

public sealed class SharedCode
{
    private readonly HashSet<(CodePlacement, string)> _cache = new HashSet<(CodePlacement, string)>();

    public void Add(CodePlacement type, string value)
    {
        if (string.IsNullOrWhiteSpace(value)) //Don't add empty strings
            return;

        var key = (type, value);

        if (!_cache.Add(key))
            throw new InvalidOperationException("This code snippet already exists");
    }

    public IEnumerable<string> GetType(CodePlacement type)
    {
        foreach ((CodePlacement, string) kvp in _cache)
        {
            if (kvp.Item1 == type)
                yield return kvp.Item2;
        }
    }

    public void Clear() => _cache.Clear();
}