using Genbox.FastData.Extensions;
using Genbox.FastData.Generator.Framework.Interfaces;

namespace Genbox.FastData.Generator.CSharp.Internal.Framework;

internal class CSharpHashDef : IHashDef
{
    public string GetHashSource(DataType dataType, string typeName) =>
            $$"""
                  [MethodImpl(MethodImplOptions.AggressiveInlining)]
                  private static uint Hash({{typeName}} value)
                  {
              {{GetHash(dataType)}}
                  }
              """;

    private static string GetHash(DataType type)
    {
        if (type == DataType.String)
        {
            return """
                            uint hash = 352654597;

                            ref char ptr = ref MemoryMarshal.GetReference(value.AsSpan());
                            int len = value.Length;

                            while (len-- > 0)
                            {
                                hash = (((hash << 5) | (hash >> 27)) + hash) ^ ptr;
                                ptr = ref Unsafe.Add(ref ptr, 1);
                            }

                            return 352654597 + (hash * 1566083941);
                   """;
        }

        return $"        return unchecked((uint)(value{(type.IsIdentityHash() ? "" : ".GetHashCode()")}));";
    }
}