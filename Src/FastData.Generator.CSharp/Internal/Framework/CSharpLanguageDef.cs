using Genbox.FastData.Generator.Framework.Definitions;
using Genbox.FastData.Generator.Framework.Interfaces;

namespace Genbox.FastData.Generator.CSharp.Internal.Framework;

internal class CSharpLanguageDef : ILanguageDef
{
    public GeneratorEncoding Encoding => GeneratorEncoding.UTF16;
    public string ArraySizeType => "uint";

    public IList<ITypeDef> TypeDefinitions => new List<ITypeDef>
    {
        new IntegerTypeDef<char>("char", char.MinValue, char.MaxValue, "char.MinValue", "char.MaxValue", x => $"'{x.ToString(NumberFormatInfo.InvariantInfo)}'"),
        new IntegerTypeDef<sbyte>("sbyte", sbyte.MinValue, sbyte.MaxValue, "sbyte.MinValue", "sbyte.MaxValue"),
        new IntegerTypeDef<byte>("byte", byte.MinValue, byte.MaxValue, "byte.MinValue", "byte.MaxValue"),
        new IntegerTypeDef<short>("short", short.MinValue, short.MaxValue, "short.MinValue", "short.MaxValue"),
        new IntegerTypeDef<ushort>("ushort", ushort.MinValue, ushort.MaxValue, "ushort.MinValue", "ushort.MaxValue"),
        new IntegerTypeDef<int>("int", int.MinValue, int.MaxValue, "int.MinValue", "int.MaxValue"),
        new IntegerTypeDef<uint>("uint", uint.MinValue, uint.MaxValue, "uint.MinValue", "uint.MaxValue", static x => x.ToString(NumberFormatInfo.InvariantInfo) + "u"),
        new IntegerTypeDef<long>("long", long.MinValue, long.MaxValue, "long.MinValue", "long.MaxValue", static x => x.ToString(NumberFormatInfo.InvariantInfo) + "l"),
        new IntegerTypeDef<ulong>("ulong", ulong.MinValue, ulong.MaxValue, "ulong.MinValue", "ulong.MaxValue", static x => x.ToString(NumberFormatInfo.InvariantInfo) + "ul"),
        new IntegerTypeDef<float>("float", float.MinValue, float.MaxValue, "float.MinValue", "float.MaxValue", static x => x.ToString(NumberFormatInfo.InvariantInfo) + "f"),
        new IntegerTypeDef<double>("double", double.MinValue, double.MaxValue, "double.MinValue", "double.MaxValue", static x => x.ToString("0.0", NumberFormatInfo.InvariantInfo)),
        new StringTypeDef("string")
    };
}