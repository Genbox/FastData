using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Generators;

namespace Genbox.FastData;

public static class FastDataGenerator
{
    public static void Generate(StringBuilder sb, FastDataConfig config)
    {
        AppendHeader(sb, config);
        AppendDataStructure(sb, config);
        AppendFooter(sb, config);
    }

    private static void AppendHeader(StringBuilder sb, FastDataConfig config)
    {
        string? ns = config.Namespace != null ? $"namespace {config.Namespace};\n" : null;
        string cn = config.Name;
        string visibility = config.ClassVisibility.ToString().ToLowerInvariant();

        string type = config.ClassType switch
        {
            ClassType.Static => " static class",
            ClassType.Instance => " class",
            ClassType.Struct => " struct",
            _ => throw new InvalidOperationException("Invalid type: " + config.ClassType)
        };

        string? attr = config.ClassType == ClassType.Struct ? "[StructLayout(LayoutKind.Auto)]" : null;
        string? iface = config.ClassType != ClassType.Static ? " : IFastSet" : null;
        string? partial = config.ClassType != ClassType.Static ? " partial" : null;

        sb.AppendLine("// <auto-generated />");

#if RELEASE
        System.Reflection.AssemblyName name = typeof(FastDataGenerator).Assembly.GetName();
        sb.Append("// Generated by ").Append(name.Name).Append(' ').AppendLine(name.Version.ToString());
        sb.Append("// Generated on: ").AppendFormat(System.Globalization.DateTimeFormatInfo.InvariantInfo, "{0:yyyy-MM-dd HH:mm:ss}", DateTime.UtcNow).AppendLine(" UTC");
#endif

        sb.Append($$"""
                    #nullable enable
                    using Genbox.FastData;
                    using Genbox.FastData.Abstracts;
                    using Genbox.FastData.Helpers;
                    using System.Runtime.InteropServices;
                    using System.Runtime.CompilerServices;
                    using System.Text;
                    using System;

                    {{ns}}
                    {{attr}}{{visibility}}{{partial}}{{type}} {{cn}} {{iface}}
                    {

                    """);
    }

    private static void AppendDataStructure(StringBuilder sb, FastDataConfig config)
    {
        //No matter the StorageMode, if there is only a single item, we will use the same data structure
        if (config.Data.Length == 1)
        {
            Generate(DataStructure.SingleValue, sb, config);
            return;
        }

        foreach (ICode candidate in GetDataStructureCandidates(config))
        {
            if (candidate.TryCreate())
            {
                sb.Append(candidate.Generate());
                break;
            }
        }
    }

    private static IEnumerable<ICode> GetDataStructureCandidates(FastDataConfig config)
    {
        GeneratorContext context = new GeneratorContext(config);

        switch (config.StorageMode)
        {
            case StorageMode.Auto:

                // For small amounts of data, logic is the fastest, so we try that first
                yield return new SwitchCode(config, context);

                // We try (unique) key lengths
                yield return new UniqueKeyLengthCode(config, context);
                yield return new KeyLengthCode(config, context);

                if (config.StorageOptions.HasFlag(StorageOption.OptimizeForMemory) && config.StorageOptions.HasFlag(StorageOption.OptimizeForSpeed))
                    yield return new MinimalPerfectHashCode(config, context);

                if (config.StorageOptions.HasFlag(StorageOption.OptimizeForMemory))
                    yield return new BinarySearchCode(config, context);
                else
                    yield return new HashSetCode(config, context, HashSetCode.HashSetType.Chain);

                break;
            case StorageMode.Linear:
                yield return new ArrayCode(config, context);
                break;
            case StorageMode.Logic:
                yield return new SwitchCode(config, context);
                break;
            case StorageMode.Tree:
                yield return new BinarySearchCode(config, context);
                break;
            case StorageMode.Indexed:
                yield return new HashSetCode(config, context, HashSetCode.HashSetType.Chain);
                break;

            default:
                throw new InvalidOperationException($"Unsupported StorageMode {config.StorageMode}");
        }
    }

    private static void AppendFooter(StringBuilder sb, FastDataConfig config)
    {
        sb.Append($"""


                       public const int ItemCount = {config.Data.Length};
                   """);

        if (config.ClassType == ClassType.Instance)
        {
            sb.Append($"""

                           public int Length => {config.Data.Length};
                       """);
        }

        sb.AppendLine();
        sb.Append('}');
    }

    /// <summary>This method is used by tests</summary>
    internal static void Generate(DataStructure ds, StringBuilder sb, FastDataConfig config)
    {
        GeneratorContext context = new GeneratorContext(config);

        ICode instance = ds switch
        {
            DataStructure.Array => new ArrayCode(config, context),
            DataStructure.BinarySearch => new BinarySearchCode(config, context),
            DataStructure.EytzingerSearch => new EytzingerSearchCode(config, context),
            DataStructure.Switch => new SwitchCode(config, context),
            DataStructure.SwitchHashCode => new SwitchHashCode(config, context),
            DataStructure.MinimalPerfectHash => new MinimalPerfectHashCode(config, context),
            DataStructure.HashSetChain => new HashSetCode(config, context, HashSetCode.HashSetType.Chain),
            DataStructure.HashSetLinear => new HashSetCode(config, context, HashSetCode.HashSetType.Linear),
            DataStructure.UniqueKeyLength => new UniqueKeyLengthCode(config, context),
            DataStructure.UniqueKeyLengthSwitch => new UniqueKeyLengthSwitchCode(config, context),
            DataStructure.KeyLength => new KeyLengthCode(config, context),
            DataStructure.SingleValue => new SingleValueCode(config, context),
            DataStructure.Conditional => new ConditionalCode(config, context),
            _ => throw new ArgumentOutOfRangeException(nameof(ds), ds, null)
        };

        if (instance.TryCreate())
            sb.Append(instance.Generate());
    }
}