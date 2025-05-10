namespace Genbox.FastData.Generator.Framework.Interfaces.Specs;

public interface ICodeSpec
{
    string GetFieldModifier();
    string GetMethodModifier();
    string GetMethodAttributes();
    string GetModFunction(string variable, ulong value);
    string GetEqualFunction(string var1, string var2);
}