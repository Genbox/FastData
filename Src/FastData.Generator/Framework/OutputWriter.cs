using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Abstracts;
using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generators;

namespace Genbox.FastData.Generator.Framework;

public abstract class OutputWriter<T> : IOutputWriter
{
    private IEarlyExitDef _earlyExitDef = null!;
    private IHashDef _hashDef = null!;
    private ILanguageDef _langDef = null!;
    private TypeHelper _typeHelper = null!;

    protected string TypeName { get; private set; } = null!;
    protected GeneratorConfig<T> GeneratorConfig { get; private set; } = null!;

    protected string EarlyExits => _earlyExitDef.GetEarlyExits<T>(GeneratorConfig.EarlyExits);
    protected string HashSource => _hashDef.GetHashSource(GeneratorConfig.DataType, TypeName);
    protected string HashSizeType => _typeHelper.GetTypeName(typeof(ulong));
    protected static DataType HashSizeDataType => DataType.UInt64;
    protected string ArraySizeType => _langDef.ArraySizeType;

    public abstract string Generate();

    internal void Initialize(ILanguageDef langDef, IEarlyExitDef earlyExitDef, TypeHelper typeHelper, IHashDef hashDef, GeneratorConfig<T> genCfg, string typeName)
    {
        _langDef = langDef;
        _earlyExitDef = earlyExitDef;
        _typeHelper = typeHelper;
        _hashDef = hashDef;
        GeneratorConfig = genCfg;
        TypeName = typeName;
    }

    protected virtual string GetEqualFunction(string value1, string value2, DataType dataType = DataType.Null) => $"{value1} == {value2}";
    protected virtual string GetModFunction(string variable, ulong value) => $"{variable} % {value}";

    protected string ToValueLabel<T2>(T2 value) => _typeHelper.ToValueLabel(value);
    protected string GetSmallestSignedType(long value) => _typeHelper.GetSmallestIntType(value);
    protected string GetSmallestUnsignedType(long value) => _typeHelper.GetSmallestUIntType((ulong)value);
}