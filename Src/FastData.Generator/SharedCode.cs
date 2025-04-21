// ReSharper disable CanSimplifyDictionaryLookupWithTryAdd
namespace Genbox.FastData.Generator;

public sealed class SharedCode
{
    private readonly Dictionary<CodeKey, string> _cache = new Dictionary<CodeKey, string>();

    private SharedCode() {}

    public static SharedCode Instance { get; } = new SharedCode();

    public void Add(string id, CodeType type, string value)
    {
        CodeKey key = new CodeKey(id, type);

        if (_cache.ContainsKey(key))
            return;

        _cache.Add(key, value);
    }

    public IEnumerable<string> GetType(CodeType type)
    {
        foreach (KeyValuePair<CodeKey, string> kvp in _cache)
        {
            if (kvp.Key.Type == type)
                yield return kvp.Value;
        }
    }

    public void Clear() => _cache.Clear();

    private readonly struct CodeKey : IEquatable<CodeKey>
    {
        public CodeKey(string id, CodeType type)
        {
            Id = id;
            Type = type;
        }

        public bool Equals(CodeKey other) => Id == other.Id && Type == other.Type;
        public override bool Equals(object? obj) => obj is CodeKey other && Equals(other);
        public override int GetHashCode() => unchecked((Id.GetHashCode() * 397) ^ (int)Type);

        public string Id { get; }
        public CodeType Type { get; }
    }
}