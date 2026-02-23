using System.Collections;
using Genbox.FastData.Enums;
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
    private GeneratorConfig<TKey> _generatorConfig = null!;
    private TypeMap _typeMap = null!;

    protected SharedCode Shared { get; private set; } = null!;
    protected bool IgnoreCase => _generatorConfig.IgnoreCase;
    protected GeneratorEncoding Encoding => _generatorConfig.Encoding;
    protected string KeyTypeName { get; private set; } = null!;
    protected string ValueTypeName { get; private set; } = null!;
    protected string HashSource { get; private set; } = null!;
    public string HashSizeType => _typeMap.GetTypeName(typeof(ulong));
    public string ArraySizeType => _langDef.ArraySizeType;
    protected string TrimPrefix => _generatorConfig.TrimPrefix;
    protected string TrimSuffix => _generatorConfig.TrimSuffix;
    protected int TotalTrimLength => TrimPrefix.Length + TrimSuffix.Length;
    public string InputKeyName => "key";
    protected static string TrimmedKeyName => "trimmedKey";
    public string LookupKeyName => TotalTrimLength == 0 ? InputKeyName : TrimmedKeyName;

    public abstract string Generate();

    protected virtual string GetMethodHeader(MethodType methodType)
    {
        return _earlyExitDef.GetEarlyExits<TKey>(_generatorConfig.EarlyExits, methodType, _generatorConfig.IgnoreCase, _generatorConfig.Encoding, Shared);
    }

    internal void Initialize(ILanguageDef langDef, IEarlyExitDef earlyExitDef, TypeMap map, IHashDef hashDef, GeneratorConfig<TKey> genCfg, string keyTypeName, string valueTypeName, ExpressionCompiler? compiler, SharedCode shared)
    {
        _langDef = langDef;
        _earlyExitDef = earlyExitDef;
        _typeMap = map;
        _generatorConfig = genCfg;
        Shared = shared;
        KeyTypeName = keyTypeName;
        ValueTypeName = valueTypeName;

        //If there is no compiler, or there is no specialized string hash, we give null to the hash definition
        StringHashInfo? stringHash = null;

        if (_generatorConfig.HashDetails.StringHash != null && compiler != null)
        {
            //We convert state from State to StateInfo such that consumers can use it directly
            StateInfo[]? genState = null;
            State[]? fdState = _generatorConfig.HashDetails.StringHash.State;

            if (fdState != null)
            {
                //We convert from State to an easily consumable StateInfo
                genState = new StateInfo[fdState.Length];

                for (int i = 0; i < fdState.Length; i++)
                {
                    State state = fdState[i];
                    genState[i] = new StateInfo(state.Name, _typeMap.GetTypeName(state.Type), GetValues(state.Values, _typeMap, state.Type).ToArray());
                }
            }

            stringHash = new StringHashInfo(compiler.GetCode(_generatorConfig.HashDetails.StringHash.Expression), _generatorConfig.HashDetails.StringHash.Functions, genState);
        }

        HashInfo hashInfo = new HashInfo(_generatorConfig.HashDetails.HasZeroOrNaN, stringHash);
        HashSource = hashDef.GetHashSource(typeof(TKey), KeyTypeName, hashInfo);
        RegisterSharedCode();
    }

    protected string GetEqualFunction(string value1, string value2, TypeCode keyTypeOverride = TypeCode.Empty)
    {
        if (keyTypeOverride == TypeCode.Empty)
            keyTypeOverride = Type.GetTypeCode(typeof(TKey));

        return GetEqualFunctionInternal(value1, value2, keyTypeOverride);
    }

    protected virtual string GetEqualFunctionInternal(string value1, string value2, TypeCode keyType) => $"{value1} == {value2}";

    protected virtual string GetModFunction(string variable, ulong value) => $"{variable} % {value}";

    protected string ToValueLabel<T>(T value) => _typeMap.ToValueLabel(value); //Uses its own generics here, not TKey. We need T so we can change the type in generators
    protected string GetObjectDeclarations<TValue>() => typeof(TValue).IsPrimitive ? "" : _typeMap.GetDeclarations<TValue>(); //We don't have declarations for primitives
    protected string GetSmallestSignedType(long value) => _generatorConfig.TypeReductionEnabled ? _typeMap.GetSmallestIntType(value) : _typeMap.Get<int>().Name;
    protected string GetSmallestUnsignedType(long value) => _generatorConfig.TypeReductionEnabled ? _typeMap.GetSmallestUIntType((ulong)value) : _typeMap.Get<uint>().Name;

    private static IEnumerable<string> GetValues(Array array, TypeMap map, Type type)
    {
        IEnumerator enumerator = array.GetEnumerator();

        while (enumerator.MoveNext())
            yield return map.ToValueLabel(enumerator.Current, type);
    }

    protected virtual void RegisterSharedCode() {}
}