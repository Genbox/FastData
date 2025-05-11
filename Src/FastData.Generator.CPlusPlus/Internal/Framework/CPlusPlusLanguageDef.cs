using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Framework.Interfaces;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Framework;

internal class CPlusPlusLanguageDef : ILanguageDef
{
    public bool UseUTF16Encoding => false;
    public string ArraySizeType => "size_t";

    public IList<ITypeDef> TypeDefinitions => new List<ITypeDef>
    {
        new IntegerTypeDef<char>("char", char.MinValue, (char)127, "0", "127", x => ((byte)x).ToString(NumberFormatInfo.InvariantInfo)),
        new IntegerTypeDef<sbyte>("int8_t", sbyte.MinValue, sbyte.MaxValue, "std::numeric_limits<int8_t>::lowest()", "std::numeric_limits<int8_t>::max()"),
        new IntegerTypeDef<byte>("uint8_t", byte.MinValue, byte.MaxValue, "0", "std::numeric_limits<uint8_t>::max()"),
        new IntegerTypeDef<short>("int16_t", short.MinValue, short.MaxValue, "std::numeric_limits<int16_t>::lowest()", "std::numeric_limits<int16_t>::max()"),
        new IntegerTypeDef<ushort>("uint16_t", ushort.MinValue, ushort.MaxValue, "0", "std::numeric_limits<uint16_t>::max()"),
        new IntegerTypeDef<int>("int32_t", int.MinValue, int.MaxValue, "std::numeric_limits<int32_t>::lowest()", "std::numeric_limits<int32_t>::max()"),
        new IntegerTypeDef<uint>("uint32_t", uint.MinValue, uint.MaxValue, "0", "std::numeric_limits<uint32_t>::max()", x => x.ToString(NumberFormatInfo.InvariantInfo) + "u"),
        new IntegerTypeDef<long>("int64_t", long.MinValue, long.MaxValue, "std::numeric_limits<int64_t>::lowest()", "std::numeric_limits<int64_t>::max()", x => x.ToString(NumberFormatInfo.InvariantInfo) + "ll"),
        new IntegerTypeDef<ulong>("uint64_t", ulong.MinValue, ulong.MaxValue, "0", "std::numeric_limits<uint64_t>::max()", x => x.ToString(NumberFormatInfo.InvariantInfo) + "ull"),
        new IntegerTypeDef<float>("float", float.MinValue, float.MaxValue, "std::numeric_limits<float>::lowest()", "std::numeric_limits<float>::max()", x => x.ToString("0.0", NumberFormatInfo.InvariantInfo) + "f"),
        new IntegerTypeDef<double>("double", double.MinValue, double.MaxValue, "std::numeric_limits<double>::lowest()", "std::numeric_limits<double>::max()", x => x.ToString("0.0", NumberFormatInfo.InvariantInfo)),
        new StringTypeDef<string>("std::string_view"),
        new BoolTypeDef<bool>("bool")
    };
}