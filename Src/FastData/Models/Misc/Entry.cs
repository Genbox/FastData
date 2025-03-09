using System.Runtime.InteropServices;

namespace Genbox.FastData.Models.Misc;

[StructLayout(LayoutKind.Auto)]
public struct Entry
{
    public uint HashCode;
    public int Next;
    public object Value;
}