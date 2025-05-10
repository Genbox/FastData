using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generator.Framework.Interfaces.Specs;

namespace Genbox.FastData.Generator.Framework;

public class IntegerTypeSpec<T>(string name, string minValue, string maxValue, Func<T, string>? toString = null, IntegerTypeFlags flags = IntegerTypeFlags.None)
    : ITypeSpec<T>, IIntegerTypeSpec
{
    public DataType DataType => (DataType)Type.GetTypeCode(typeof(T));
    public string Name { get; } = name;
    public string MinValue { get; } = minValue;
    public string MaxValue { get; } = maxValue;
    public IntegerTypeFlags Flags { get; } = flags;
}