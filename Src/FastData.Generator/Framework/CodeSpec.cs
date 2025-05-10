using Genbox.FastData.Generator.Framework.Interfaces.Specs;

namespace Genbox.FastData.Generator.Framework;

public abstract class CodeSpec : ICodeSpec
{
    public virtual string GetFieldModifier() => string.Empty;
    public virtual string GetMethodModifier() => string.Empty;
    public virtual string GetMethodAttributes() => string.Empty;

    public virtual string GetModFunction(string variable, ulong value) => $"{variable} % {value}";
    public virtual string GetEqualFunction(string var1, string var2) => $"{var1} == {var2}";
}