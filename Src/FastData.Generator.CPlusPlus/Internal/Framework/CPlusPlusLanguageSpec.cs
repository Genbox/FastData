using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generator.Framework.Interfaces.Specs;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Framework;

internal class CPlusPlusLanguageSpec : ILanguageSpec
{
    public bool UseUTF16Encoding => false;
    public string CommentChar => "//";
    public string AssignmentChar => "=";
    public string ArraySizeType => "size_t";

    public IList<ITypeSpec> Primitives => new List<ITypeSpec>
    {
        new IntegerTypeSpec<char>("char", char.MinValue, (char)127, "0", "127", x => ((byte)x).ToString(NumberFormatInfo.InvariantInfo)),
        new IntegerTypeSpec<sbyte>("int8_t", sbyte.MinValue, sbyte.MaxValue, "std::numeric_limits<int8_t>::lowest()", "std::numeric_limits<int8_t>::max()"),
        new IntegerTypeSpec<byte>("uint8_t", byte.MinValue, byte.MaxValue, "0", "std::numeric_limits<uint8_t>::max()"),
        new IntegerTypeSpec<short>("int16_t", short.MinValue, short.MaxValue, "std::numeric_limits<int16_t>::lowest()", "std::numeric_limits<int16_t>::max()"),
        new IntegerTypeSpec<ushort>("uint16_t", ushort.MinValue, ushort.MaxValue, "0", "std::numeric_limits<uint16_t>::max()"),
        new IntegerTypeSpec<int>("int32_t", int.MinValue, int.MaxValue, "std::numeric_limits<int32_t>::lowest()", "std::numeric_limits<int32_t>::max()", flags: IntegerTypeFlags.Default),
        new IntegerTypeSpec<uint>("uint32_t", uint.MinValue, uint.MaxValue, "0", "std::numeric_limits<uint32_t>::max()", x => x.ToString(NumberFormatInfo.InvariantInfo) + "u"),
        new IntegerTypeSpec<long>("int64_t", long.MinValue, long.MaxValue, "std::numeric_limits<int64_t>::lowest()", "std::numeric_limits<int64_t>::max()", x => x.ToString(NumberFormatInfo.InvariantInfo) + "ll"),
        new IntegerTypeSpec<ulong>("uint64_t", ulong.MinValue, ulong.MaxValue, "0", "std::numeric_limits<uint64_t>::max()", x => x.ToString(NumberFormatInfo.InvariantInfo) + "ull"),
        new IntegerTypeSpec<float>("float", float.MinValue, float.MaxValue, "std::numeric_limits<float>::lowest()", "std::numeric_limits<float>::max()", x => x.ToString("0.0", NumberFormatInfo.InvariantInfo) + "f"),
        new IntegerTypeSpec<double>("double", double.MinValue, double.MaxValue, "std::numeric_limits<double>::lowest()", "std::numeric_limits<double>::max()", x => x.ToString("0.0", NumberFormatInfo.InvariantInfo)),
        new StringTypeSpec<string>("std::string_view"),
        new BoolTypeSpec<bool>("bool"),
    };
}