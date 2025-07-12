using System.Collections;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Abstracts;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.StringHash.Framework;

namespace Genbox.FastData.Generator.Framework;

public abstract class OutputWriter<T> : IOutputWriter
{
    private IEarlyExitDef _earlyExitDef = null!;
    private ILanguageDef _langDef = null!;
    private TypeMap _map = null!;

    protected string KeyTypeName { get; private set; } = null!;
    protected string ValueTypeName { get; private set; } = null!;
    protected GeneratorConfig<T> GeneratorConfig { get; private set; } = null!;
    protected string HashSource { get; private set; } = null!;
    protected string EarlyExits => _earlyExitDef.GetEarlyExits<T>(GeneratorConfig.EarlyExits);
    protected string HashSizeType => _map.GetTypeName(typeof(ulong));
    protected static DataType HashSizeDataType => DataType.UInt64;
    protected string ArraySizeType => _langDef.ArraySizeType;

    public abstract string Generate();

    internal void Initialize(ILanguageDef langDef, IEarlyExitDef earlyExitDef, TypeMap map, IHashDef hashDef, GeneratorConfig<T> genCfg, string keyTypeName, string valueTypeName, ExpressionCompiler? compiler)
    {
        _langDef = langDef;
        _earlyExitDef = earlyExitDef;
        _map = map;
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
                    genState[i] = new StateInfo(state.Name, _map.GetTypeName(state.Type), GetValues(state.Values, _map, state.Type).ToArray());
                }
            }

            stringHash = new StringHashInfo(compiler.GetCode(GeneratorConfig.HashDetails.StringHash.Expression), GeneratorConfig.HashDetails.StringHash.Functions, genState);
        }

        HashInfo hashInfo = new HashInfo(GeneratorConfig.HashDetails.HasZeroOrNaN, stringHash);
        HashSource = hashDef.GetHashSource(GeneratorConfig.DataType, KeyTypeName, hashInfo);
    }

    private static IEnumerable<string> GetValues(Array array, TypeMap map, Type type)
    {
        IEnumerator enumerator = array.GetEnumerator();

        while (enumerator.MoveNext())
            yield return map.ToValueLabel(enumerator.Current, type);
    }

    protected virtual string GetEqualFunction(string value1, string value2, DataType dataType = DataType.Null) => $"{value1} == {value2}";
    protected virtual string GetModFunction(string variable, ulong value) => $"{variable} % {value}";

    protected string ToValueLabel<T2>(T2 value) => _map.ToValueLabel(value);
    protected string GetSmallestSignedType(long value) => _map.GetSmallestIntType(value);
    protected string GetSmallestUnsignedType(long value) => _map.GetSmallestUIntType((ulong)value);
}