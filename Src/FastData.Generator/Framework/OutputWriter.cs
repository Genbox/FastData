using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generator.Framework.Interfaces.Specs;

namespace Genbox.FastData.Generator.Framework;

public abstract class OutputWriter<T> : IOutputWriter
{
    private ICodeSpec _codeSpec;
    private ILanguageSpec _langSpec;
    private IEarlyExitHandler _earlyExitHandler;
    private CodeHelper _codeHelper;
    private IHashHandler _hashHandler;
    private string _typeName;
    private GeneratorConfig<T> _genCfg;

    internal void Initialize(ICodeSpec codeSpec,
                             ILanguageSpec langSpec,
                             IEarlyExitHandler earlyExitHandler,
                             CodeHelper codeHelper,
                             IHashHandler hashHandler,
                             GeneratorConfig<T> genCfg,
                             string typeName)
    {
        _codeSpec = codeSpec;
        _langSpec = langSpec;
        _earlyExitHandler = earlyExitHandler;
        _codeHelper = codeHelper;
        _hashHandler = hashHandler;
        _typeName = typeName;
        _genCfg = genCfg;
    }

    public abstract string Generate();

    protected string GetTypeName() => _typeName;
    protected string GetEarlyExits() => _earlyExitHandler.GetEarlyExits<T>(_genCfg.EarlyExits);
    protected string GetHashSource() => _hashHandler.GetHashSource(_genCfg.DataType, _typeName);

    protected string GetArraySizeType() => _langSpec.ArraySizeType;

    protected string GetFieldModifier() => _codeSpec.GetFieldModifier();
    protected string GetMethodModifier() => _codeSpec.GetMethodModifier();
    protected string GetMethodAttributes() => _codeSpec.GetMethodAttributes();
    protected string GetEqualFunction(string value1, string value2) => _codeSpec.GetEqualFunction(value1, value2);
    protected string GetModFunction(string variable, long value) => _codeSpec.GetModFunction(variable, (ulong)value);

    protected string ToValueLabel<T2>(T2 value) => _codeHelper.ToValueLabel(value);
    protected string GetSmallestSignedType(long value) => _codeHelper.GetSmallestIntType(value);
    protected string GetSmallestUnsignedType(long value) => _codeHelper.GetSmallestUIntType((ulong)value);
}