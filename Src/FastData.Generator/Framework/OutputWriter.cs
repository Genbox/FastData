using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Framework.Interfaces;

namespace Genbox.FastData.Generator.Framework;

public abstract class OutputWriter<T> : IOutputWriter
{
    private ILanguageDef _langDef = null!;
    private IEarlyExitDef _earlyExitDef = null!;
    private TypeHelper _typeHelper = null!;
    private IHashDef _hashDef = null!;
    private ExpressionCompiler? _compiler;

    internal void Initialize(ILanguageDef langDef, IEarlyExitDef earlyExitDef, TypeHelper typeHelper, IHashDef hashDef, GeneratorConfig<T> genCfg, string typeName, ExpressionCompiler? compiler)
    {
        _langDef = langDef;
        _earlyExitDef = earlyExitDef;
        _typeHelper = typeHelper;
        _hashDef = hashDef;
        GeneratorConfig = genCfg;
        TypeName = typeName;
        _compiler = compiler;
    }

    protected string TypeName { get; private set; } = null!;
    protected GeneratorConfig<T> GeneratorConfig { get; private set; } = null!;

    public abstract string Generate();

    protected string EarlyExits => _earlyExitDef.GetEarlyExits<T>(GeneratorConfig.EarlyExits);
    protected string HashSource => _hashDef.GetHashSource(GeneratorConfig.DataType, TypeName, GeneratorConfig.DataType == DataType.String && _compiler != null && GeneratorConfig.StringHash != null ? _compiler.GetCode(GeneratorConfig.StringHash.GetExpression()) : null);
    protected string HashSizeType => _typeHelper.GetTypeName(typeof(ulong));
    protected static DataType HashSizeDataType => DataType.UInt64;
    protected string ArraySizeType => _langDef.ArraySizeType;

    protected virtual string GetEqualFunction(string value1, string value2, DataType dataType = DataType.Null) => $"{value1} == {value2}";
    protected virtual string GetModFunction(string variable, ulong value) => $"{variable} % {value}";

    protected string ToValueLabel<T2>(T2 value) => _typeHelper.ToValueLabel(value);
    protected string GetSmallestSignedType(long value) => _typeHelper.GetSmallestIntType(value);
    protected string GetSmallestUnsignedType(long value) => _typeHelper.GetSmallestUIntType((ulong)value);
}