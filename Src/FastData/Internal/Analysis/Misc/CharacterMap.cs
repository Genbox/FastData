using System.Runtime.InteropServices;

namespace Genbox.FastData.Internal.Analysis.Misc;

[StructLayout(LayoutKind.Auto)]
internal readonly record struct CharacterMap
{
    private readonly int[] _map = new int[255];

    public CharacterMap() {}

    public char MinChar
    {
        get
        {
            for (int i = 0; i < _map.Length; i++)
            {
                if (_map[i] != 0)
                    return (char)i;
            }

            return '\0';
        }
    }

    public char MaxChar
    {
        get
        {
            for (int i = _map.Length - 1; i >= 0; i--)
            {
                if (_map[i] != 0)
                    return (char)i;
            }

            return '\0';
        }
    }

    internal void Add(char c) => _map[c]++;
    internal bool Contains(char c) => _map[c] != 0;
}