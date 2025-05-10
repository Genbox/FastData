using Genbox.FastData.Enums;

namespace Genbox.FastData.Generator.Framework.Interfaces;

public interface IHashHandler
{
    string GetHashSource(DataType dataType, string typeName);
}