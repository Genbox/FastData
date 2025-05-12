using Genbox.FastData.Enums;

namespace Genbox.FastData.Generator.Framework.Interfaces;

public interface IHashDef
{
    string GetHashSource(DataType dataType, string typeName, bool use64BitHashing);
}