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

    protected SharedCode Shared { get; private set; } = null!;
    protected TypeMap TypeMap { get; private set; } = null!;
    protected string KeyTypeName { get; private set; } = null!;
    protected string ValueTypeName { get; private set; } = null!;
    protected GeneratorConfig<TKey> GeneratorConfig { get; private set; } = null!;
    protected string HashSource { get; private set; } = null!;
    protected string HashSizeType => TypeMap.GetTypeName(typeof(ulong));
    protected string ArraySizeType => _langDef.ArraySizeType;
    protected string TrimPrefix => GeneratorConfig.TrimPrefix;
    protected string TrimSuffix => GeneratorConfig.TrimSuffix;
    protected int TotalTrimLength => TrimPrefix.Length + TrimSuffix.Length;
    protected string LookupKeyName => TotalTrimLength == 0 ? "key" : "trimmedKey";

    public abstract string Generate();

    protected virtual string GetMethodHeader(MethodType methodType)
    {
        return _earlyExitDef.GetEarlyExits<TKey>(GeneratorConfig.EarlyExits, methodType);
    }

    internal void Initialize<TValue>(ILanguageDef langDef, IEarlyExitDef earlyExitDef, TypeMap map, IHashDef hashDef, GeneratorConfig<TKey> genCfg, string keyTypeName, string valueTypeName, ReadOnlyMemory<TValue> values, ExpressionCompiler? compiler, SharedCode shared)
    {
        _langDef = langDef;
        _earlyExitDef = earlyExitDef;
        Shared = shared;
        TypeMap = map;
        GeneratorConfig = genCfg;
        KeyTypeName = keyTypeName;
        ValueTypeName = valueTypeName;

        //If there is no compiler, or there is no specialized string hash, we give null to the hash definition
        StringHashInfo? stringHash = null;

        if (GeneratorConfig.HashDetails.StringHash != null && compiler != null)
        {
            //We convert state from State to StateInfo such that consumers can use it directly
            StateInfo[]? genState = null;
            State[]? fdState = GeneratorConfig.HashDetails.StringHash.State;

            if (fdState != null)
            {
                //We convert from State to an easily consumable StateInfo
                genState = new StateInfo[fdState.Length];

                for (int i = 0; i < fdState.Length; i++)
                {
                    State state = fdState[i];
                    genState[i] = new StateInfo(state.Name, TypeMap.GetTypeName(state.Type), GetValues(state.Values, TypeMap, state.Type).ToArray());
                }
            }

            stringHash = new StringHashInfo(compiler.GetCode(GeneratorConfig.HashDetails.StringHash.Expression), GeneratorConfig.HashDetails.StringHash.Functions, genState);
        }

        HashInfo hashInfo = new HashInfo(GeneratorConfig.HashDetails.HasZeroOrNaN, stringHash);
        HashSource = hashDef.GetHashSource(GeneratorConfig.KeyType, KeyTypeName, hashInfo);
        RegisterSharedCode();
    }

    protected string GetEqualFunction(string value1, string value2, KeyType keyTypeOverride = KeyType.Null)
    {
        if (keyTypeOverride == KeyType.Null)
            keyTypeOverride = GeneratorConfig.KeyType;

        return GetEqualFunctionInternal(value1, value2, keyTypeOverride);
    }

    protected virtual string GetEqualFunctionInternal(string value1, string value2, KeyType keyType) => $"{value1} == {value2}";

    protected virtual string GetModFunction(string variable, ulong value) => $"{variable} % {value}";

    protected string ToValueLabel<T>(T value) => TypeMap.ToValueLabel(value); //Uses its own generics here, not TKey. We need T so we can change the type in generators
    protected string GetObjectDeclarations<TValue>() => typeof(TValue).IsPrimitive ? "" : TypeMap.GetDeclarations<TValue>(); //We don't have declarations for primitives
    protected string GetSmallestSignedType(long value) => TypeMap.GetSmallestIntType(value);
    protected string GetSmallestUnsignedType(long value) => TypeMap.GetSmallestUIntType((ulong)value);

    private static IEnumerable<string> GetValues(Array array, TypeMap map, Type type)
    {
        IEnumerator enumerator = array.GetEnumerator();

        while (enumerator.MoveNext())
            yield return map.ToValueLabel(enumerator.Current, type);
    }

    protected virtual void RegisterSharedCode() {}
}