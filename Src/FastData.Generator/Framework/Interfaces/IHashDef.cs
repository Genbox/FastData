namespace Genbox.FastData.Generator.Framework.Interfaces;

public interface IHashDef
{
    string GetStringHashSource(string typeName);
    string GetNumericHashSource(TypeCode keyType, string typeName, bool hasZeroOrNaN);
    string Wrap(TypeCode typeCode, string typeName, string hash);
}