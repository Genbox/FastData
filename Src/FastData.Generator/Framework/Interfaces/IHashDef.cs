namespace Genbox.FastData.Generator.Framework.Interfaces;

public interface IHashDef
{
    string GetHashSource(Type keyType, string typeName, HashInfo info);
}