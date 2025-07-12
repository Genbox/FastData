// ReSharper disable CanSimplifyDictionaryLookupWithTryAdd

using Genbox.FastData.Generator.Enums;

namespace Genbox.FastData.Generator;

public sealed class SharedCode
{
    private readonly Dictionary<CodeKey, string> _cache = new Dictionary<CodeKey, string>();

    public void Add(string id, CodePlacement type, string value)
    {
        CodeKey key = new CodeKey(id, type);

        if (_cache.ContainsKey(key))
            return;

        _cache.Add(key, value);
    }

    public IEnumerable<string> GetType(CodePlacement type)
    {
        foreach (KeyValuePair<CodeKey, string> kvp in _cache)
        {
            if (kvp.Key.Type == type)
                yield return kvp.Value;
        }
    }

    public void Clear() => _cache.Clear();

    private readonly struct CodeKey(string id, CodePlacement type) : IEquatable<CodeKey>
    {
        private readonly string _id = id;

        public bool Equals(CodeKey other) => _id == other._id && Type == other.Type;
        public override bool Equals(object? obj) => obj is CodeKey other && Equals(other);
        public override int GetHashCode() => unchecked((_id.GetHashCode() * 397) ^ (int)Type);

        public CodePlacement Type { get; } = type;
    }
}