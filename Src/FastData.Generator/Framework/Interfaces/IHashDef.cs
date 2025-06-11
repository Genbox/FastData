using Genbox.FastData.Enums;
using Genbox.FastData.Generators;

namespace Genbox.FastData.Generator.Framework.Interfaces;

public interface IHashDef
{
    string GetHashSource(DataType dataType, string typeName, HashInfo info);
}