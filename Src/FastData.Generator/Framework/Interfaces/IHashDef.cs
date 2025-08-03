using Genbox.FastData.Enums;

namespace Genbox.FastData.Generator.Framework.Interfaces;

public interface IHashDef
{
    string GetHashSource(KeyType keyType, string typeName, HashInfo info);
}