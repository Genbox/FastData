using System.Text;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Generator.CSharp.Enums;
using Genbox.FastData.Generator.CSharp.Internal.Generators;
using Genbox.FastData.Models;

namespace Genbox.FastData.Generator.CSharp;

public class CSharpCodeGenerator(CSharpGeneratorConfig userCfg) : IGenerator
{
    private readonly StringBuilder _sb = new StringBuilder();

    public string Generate(GeneratorConfig genCfg, FastDataConfig fastCfg, IContext context)
    {
        _sb.Clear();
        AppendHeader(genCfg, fastCfg);

        _sb.Append(context switch
        {
            SingleValueContext c2 => new SingleValueCode(genCfg, userCfg, c2).Generate(),
            ArrayContext c1 => new ArrayCode(genCfg, userCfg, c1).Generate(),
            BinarySearchContext c2 => new BinarySearchCode(genCfg, userCfg, c2).Generate(),
            ConditionalContext c2 => new ConditionalCode(genCfg, userCfg, c2).Generate(),
            EytzingerSearchContext c2 => new EytzingerSearchCode(genCfg, userCfg, c2).Generate(),
            PerfectHashBruteForceContext c2 => new PerfectHashBruteForceCode(genCfg, userCfg, c2).Generate(),
            PerfectHashGPerfContext c2 => new PerfectHashGPerfCode(genCfg, userCfg, c2).Generate(),
            HashSetChainContext c2 => new HashSetChainCode(genCfg, userCfg, c2).Generate(),
            HashSetLinearContext c2 => new HashSetLinearCode(genCfg, userCfg, c2).Generate(),
            KeyLengthContext c2 => new KeyLengthCode(genCfg, userCfg, c2).Generate(),
            _ => throw new NotSupportedException("The context type is not supported: " + context.GetType().Name)
        });

        AppendFooter(fastCfg);
        return _sb.ToString();
    }

    private void AppendHeader(GeneratorConfig genCfg, FastDataConfig fastCfg)
    {
        string cn = fastCfg.Name;
        string? ns = userCfg.Namespace != null ? $"namespace {userCfg.Namespace};\n" : null;
        string visibility = userCfg.ClassVisibility.ToString().ToLowerInvariant();

        string type = userCfg.ClassType switch
        {
            ClassType.Static => " static class",
            ClassType.Instance => " class",
            ClassType.Struct => " struct",
            _ => throw new InvalidOperationException("Invalid type: " + userCfg.ClassType)
        };

        string? attr = userCfg.ClassType == ClassType.Struct ? "[StructLayout(LayoutKind.Auto)]" : null;
        string? iface = userCfg.ClassType != ClassType.Static ? $" : IFastSet<{genCfg.DataType}>" : null;
        string? partial = userCfg.ClassType != ClassType.Static ? " partial" : null;

        _sb.AppendLine("// <auto-generated />");

#if RELEASE
        System.Reflection.AssemblyName name = typeof(FastDataGenerator).Assembly.GetName();
        _sb.Append("// Generated by ").Append(name.Name).Append(' ').AppendLine(name.Version.ToString());
        _sb.Append("// Generated on: ").AppendFormat(System.Globalization.DateTimeFormatInfo.InvariantInfo, "{0:yyyy-MM-dd HH:mm:ss}", DateTime.UtcNow).AppendLine(" UTC");
#endif

        _sb.Append($$"""
                     #nullable enable
                     using Genbox.FastData.Abstracts;
                     using Genbox.FastData.Generator.CSharp.Abstracts;
                     using Genbox.FastData.Helpers;
                     using Genbox.FastData;
                     using System.Runtime.CompilerServices;
                     using System.Runtime.InteropServices;
                     using System.Text;
                     using System;

                     {{ns}}
                     {{attr}}{{visibility}}{{partial}}{{type}} {{cn}} {{iface}}
                     {

                     """);
    }

    private void AppendFooter(FastDataConfig fastCfg)
    {
        _sb.Append($"""


                        public const int ItemCount = {fastCfg.Data.Length};
                    """);

        if (userCfg.ClassType == ClassType.Instance)
        {
            _sb.Append($"""

                            public int Length => {fastCfg.Data.Length};
                        """);
        }

        _sb.AppendLine();
        _sb.Append('}');
    }
}