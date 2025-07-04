using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Extensions;

namespace Genbox.FastData.Generator.Framework;

public abstract class CodeGenerator : ICodeGenerator
{
    private readonly IConstantsDef _constDef;
    private readonly IEarlyExitDef _earlyExitDef;
    private readonly IHashDef _hashDef;
    private readonly ExpressionCompiler? _compiler;
    private readonly ILanguageDef _langDef;
    private readonly TypeHelper _typeHelper;
    private readonly TypeMap _typeMap;

    protected CodeGenerator(ILanguageDef langDef, IConstantsDef constDef, IEarlyExitDef earlyExitDef, IHashDef hashDef, ExpressionCompiler? compiler)
    {
        _langDef = langDef;
        _constDef = constDef;
        _earlyExitDef = earlyExitDef;
        _hashDef = hashDef;
        _compiler = compiler;

        _typeMap = new TypeMap(langDef.TypeDefinitions);
        _typeHelper = new TypeHelper(_typeMap);
        Shared = new SharedCode();
    }

    protected SharedCode Shared { get; }

    public bool UseUTF16Encoding => _langDef.UseUTF16Encoding;

    public virtual string Generate<T>(ReadOnlySpan<T> data, GeneratorConfig<T> genCfg, IContext<T> context) where T : notnull
    {
        Shared.Clear();

        string typeName = _typeMap.Get<T>().Name;

        StringBuilder sb = new StringBuilder();
        AppendHeader(sb, genCfg, context);
        AppendBody(sb, genCfg, typeName, context, data);
        AppendFooter(sb, genCfg, typeName);

        foreach (string classCode in Shared.GetType(CodeType.Class))
        {
            sb.AppendLine();
            sb.AppendLine(classCode);
        }

        return sb.ToString();
    }

    protected abstract OutputWriter<T>? GetOutputWriter<T>(GeneratorConfig<T> genCfg, IContext<T> context) where T : notnull;

    protected virtual void AppendHeader<T>(StringBuilder sb, GeneratorConfig<T> genCfg, IContext<T> context) where T : notnull
    {
        string subType = context.GetType().Name.Replace("Context`1", "");

        sb.Append(_constDef.Comment).Append(' ').AppendLine("This file is auto-generated. Do not edit manually.");
        sb.Append(_constDef.Comment).Append(' ').AppendLine($"Structure: {genCfg.StructureType}{(genCfg.StructureType.ToString() != subType ? $" ({subType})" : string.Empty)}");

#if RELEASE
        sb.Append(_constDef.Comment).Append(' ').AppendLine("Generated by: " + genCfg.Metadata.Program);
        sb.Append(_constDef.Comment).Append(' ').AppendLine("Generated on: " + genCfg.Metadata.Timestamp);
#endif
    }

    protected virtual void AppendBody<T>(StringBuilder sb, GeneratorConfig<T> genCfg, string typeName, IContext<T> context, ReadOnlySpan<T> data) where T : notnull
    {
        OutputWriter<T>? writer = GetOutputWriter(genCfg, context);

        if (writer == null)
            throw new NotSupportedException("The context type is not supported: " + context.GetType().Name);

        writer.Initialize(_langDef, _earlyExitDef, _typeHelper, _hashDef, genCfg, typeName, _compiler);
        sb.AppendLine(writer.Generate(data));
    }

    protected virtual void AppendFooter<T>(StringBuilder sb, GeneratorConfig<T> genCfg, string typeName)
    {
        sb.AppendLine();
        sb.AppendLine(_constDef.ItemCountTemplate(_langDef.ArraySizeType, genCfg.Constants.ItemCount.ToStringInvariant()));

        if (genCfg.DataType.IsInteger())
        {
            sb.AppendLine(_constDef.MinValueTemplate(typeName, _typeHelper.ToValueLabel(genCfg.Constants.MinValue)));
            sb.AppendLine(_constDef.MaxValueTemplate(typeName, _typeHelper.ToValueLabel(genCfg.Constants.MaxValue)));
        }
        else if (genCfg.DataType == DataType.String)
        {
            sb.AppendLine(_constDef.MinLengthTemplate(_langDef.ArraySizeType, genCfg.Constants.MinStringLength.ToStringInvariant()));
            sb.AppendLine(_constDef.MaxLengthTemplate(_langDef.ArraySizeType, genCfg.Constants.MaxStringLength.ToStringInvariant()));
        }
    }
}