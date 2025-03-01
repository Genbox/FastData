using System.Runtime.InteropServices;

namespace Genbox.FastData.Internal.Analysis.Properties;

[StructLayout(LayoutKind.Auto)]
internal record DataProperties
{
    public StringProperties? StringProps { get; set; }
    public IntegerProperties? IntProps { get; set; }
    public UnsignedIntegerProperties? UIntProps { get; set; }
    public CharProperties? CharProps { get; set; }
    public FloatProperties? FloatProps { get; set; }
}