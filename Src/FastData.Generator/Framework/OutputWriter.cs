using Genbox.FastData.Generator.Abstracts;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.StringHash.Framework;

namespace Genbox.FastData.Generator.Framework;

public abstract class OutputWriter<TKey> : IOutputWriter
{
    private IEarlyExitDef _earlyExitDef = null!;
    private ILanguageDef _langDef = null!;
    private TypeMap _typeMap = null!;

    protected SharedCode Shared { get; private set; } = null!;
    public GeneratorConfigBase GeneratorConfig { get; private set; } = null!;
    protected string KeyTypeName { get; private set; } = null!;
    protected string ValueTypeName { get; private set; } = null!;
    public string HashSizeType => _typeMap.GetTypeName(typeof(ulong));
    public string ArraySizeType => _langDef.ArraySizeType;
    public string InputKeyName => "key";

    public string LookupKeyName
    {
        get
        {
            if (GeneratorConfig is StringGeneratorConfig stringConfig)
            {
                if (stringConfig.TrimPrefix.Length + stringConfig.TrimSuffix.Length != 0)
                    return "trimmedKey";
            }

            return InputKeyName;
        }
    }

    protected static string TrimmedKeyName => "trimmedKey";

    public string? HashSource { get; private set; }

    public abstract string Generate();

    protected virtual string GetMethodHeader(MethodType methodType)
    {
        return _earlyExitDef.GetEarlyExits<TKey>(GeneratorConfig.EarlyExits, methodType, InputKeyName);
    }

    internal void Initialize(ILanguageDef langDef, IEarlyExitDef earlyExitDef, TypeMap map, IHashDef hashDef, GeneratorConfigBase genCfg, string keyTypeName, string valueTypeName, ExpressionCompiler? compiler, SharedCode shared)
    {
        _langDef = langDef;
        _earlyExitDef = earlyExitDef;
        _typeMap = map;
        GeneratorConfig = genCfg;
        Shared = shared;
        KeyTypeName = keyTypeName;
        ValueTypeName = valueTypeName;

        TypeCode typeCode = Type.GetTypeCode(typeof(TKey));

        // If the generator supports expressions, and we have a generated hash
        if (hashDef is IHashExpressionDef expDef && genCfg is StringGeneratorConfig strCfg && strCfg.HashInfo != null && compiler != null)
        {
            // Render helper functions and additional data
            if (strCfg.HashInfo.Functions != ReaderFunctions.None)
                Shared.Add(CodePlacement.InClass, expDef.RenderFunctions(strCfg.HashInfo.Functions));

            if (strCfg.HashInfo.AdditionalData != null)
                Shared.Add(CodePlacement.InClass, expDef.RenderAdditionalData(strCfg.HashInfo.AdditionalData));

            // Compile the expression to source
            string exprStr = compiler.GetCode(strCfg.HashInfo.Expression, 4);

            //Wrap it in a function
            HashSource = hashDef.Wrap(typeCode, keyTypeName, exprStr);
        }
        else
        {
            HashSource = hashDef.Wrap(typeCode, keyTypeName, typeCode == TypeCode.String ? hashDef.GetStringHashSource(keyTypeName) : hashDef.GetNumericHashSource(typeCode, keyTypeName, ((NumericGeneratorConfig<TKey>)GeneratorConfig).HasZeroOrNaN));
        }

        RegisterSharedCode();
    }

    protected string GetEqualFunction(string value1, string value2, TypeCode keyTypeOverride = TypeCode.Empty)
    {
        if (keyTypeOverride == TypeCode.Empty)
            keyTypeOverride = Type.GetTypeCode(typeof(TKey));

        return GetEqualFunctionInternal(value1, value2, keyTypeOverride);
    }

    protected virtual string GetEqualFunctionInternal(string value1, string value2, TypeCode overrideType) => $"{value1} == {value2}";

    protected virtual string GetModFunction(string variable, ulong value) => $"{variable} % {value}";

    protected string ToValueLabel<T>(T value) => _typeMap.ToValueLabel(value); //Uses its own generics here, not TKey. We need T so we can change the type in generators
    protected string GetObjectDeclarations<TValue>() => typeof(TValue).IsPrimitive ? "" : _typeMap.GetDeclarations<TValue>(); //We don't have declarations for primitives
    protected string GetSmallestSignedType(long value) => GeneratorConfig.TypeReductionEnabled ? _typeMap.GetSmallestIntType(value) : _typeMap.Get<int>().Name;
    protected string GetSmallestUnsignedType(long value) => GeneratorConfig.TypeReductionEnabled ? _typeMap.GetSmallestUIntType((ulong)value) : _typeMap.Get<uint>().Name;

    protected virtual void RegisterSharedCode() {}
}