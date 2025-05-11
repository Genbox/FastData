using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generator.Framework.Interfaces.Specs;

namespace Genbox.FastData.Generator.CSharp.Internal.Framework;

internal class CSharpLanguageSpec : ILanguageSpec
{
    public bool UseUTF16Encoding => true;
    public string CommentChar => "//";
    public string ArraySizeType => "uint";

    public IList<ITypeSpec> Primitives => new List<ITypeSpec>
    {
        new IntegerTypeSpec<char>("char", char.MinValue, char.MaxValue, "char.MinValue", "char.MaxValue", x => $"'{x.ToString(NumberFormatInfo.InvariantInfo)}'"),
        new IntegerTypeSpec<sbyte>("sbyte", sbyte.MinValue, sbyte.MaxValue, "sbyte.MinValue", "sbyte.MaxValue"),
        new IntegerTypeSpec<byte>("byte", byte.MinValue, byte.MaxValue, "byte.MinValue", "byte.MaxValue"),
        new IntegerTypeSpec<short>("short", short.MinValue, short.MaxValue, "short.MinValue", "short.MaxValue"),
        new IntegerTypeSpec<ushort>("ushort", ushort.MinValue, ushort.MaxValue, "ushort.MinValue", "ushort.MaxValue"),
        new IntegerTypeSpec<int>("int", int.MinValue, int.MaxValue, "int.MinValue", "int.MaxValue", flags: IntegerTypeFlags.Default),
        new IntegerTypeSpec<uint>("uint", uint.MinValue, uint.MaxValue, "uint.MinValue", "uint.MaxValue", x => x.ToString(NumberFormatInfo.InvariantInfo) + "u"),
        new IntegerTypeSpec<long>("long", long.MinValue, long.MaxValue, "long.MinValue", "long.MaxValue", x => x.ToString(NumberFormatInfo.InvariantInfo) + "l"),
        new IntegerTypeSpec<ulong>("ulong", ulong.MinValue, ulong.MaxValue, "ulong.MinValue", "ulong.MaxValue", x => x.ToString(NumberFormatInfo.InvariantInfo) + "ul"),
        new IntegerTypeSpec<float>("float", float.MinValue, float.MaxValue, "float.MinValue", "float.MaxValue", x => x.ToString(NumberFormatInfo.InvariantInfo) + "f"),
        new IntegerTypeSpec<double>("double", double.MinValue, double.MaxValue, "double.MinValue", "double.MaxValue", x => x.ToString("0.0", NumberFormatInfo.InvariantInfo)),
        new StringTypeSpec<string>("string"),
        new BoolTypeSpec<bool>("bool")
    };
}