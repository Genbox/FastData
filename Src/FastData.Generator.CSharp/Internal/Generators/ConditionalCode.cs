using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class ConditionalCode<TKey, TValue>(ConditionalContext<TKey, TValue> ctx, CSharpCodeGeneratorConfig cfg, SharedCode shared) : CSharpOutputWriter<TKey>(cfg)
{
    public override string Generate()
    {
        StringBuilder sb = new StringBuilder();
        ReadOnlySpan<TKey> keys = ctx.Keys.Span;
        ReadOnlySpan<TValue> values = ctx.Values.Span;

        if (!values.IsEmpty)
        {
            shared.Add(CodePlacement.Before, GetObjectDeclarations<TValue>());

            sb.Append($$"""
                            {{FieldModifier}}{{ValueTypeName}}[] _values = {
                        {{FormatColumns(values, ToValueLabel)}}
                            };

                        """);
        }

        return cfg.ConditionalBranchType switch
        {
            BranchType.If => GenerateIf(sb, keys),
            BranchType.Switch => GenerateSwitch(sb, keys),
            _ => throw new InvalidOperationException("Invalid branch type: " + cfg.ConditionalBranchType)
        };
    }

    private string GenerateIf(StringBuilder sb, ReadOnlySpan<TKey> data)
    {
        sb.Append($$"""
                        {{MethodAttribute}}
                        {{MethodModifier}}bool Contains({{KeyTypeName}} key)
                        {
                    {{GetMethodHeader(MethodType.Contains)}}

                            if ({{FormatList(data, x => GetEqualFunction(LookupKeyName, ToValueLabel(x)), " || ")}})
                                return true;

                            return false;
                        }
                    """);

        if (!ctx.Values.IsEmpty)
        {
            sb.Append($$"""

                            {{MethodAttribute}}
                            {{MethodModifier}}bool TryLookup({{KeyTypeName}} key, out {{ValueTypeName}}? value)
                            {
                        {{GetMethodHeader(MethodType.TryLookup)}}

                        {{GenerateBranches(data)}}

                                value = default;
                                return false;
                            }
                        """);
        }

        return sb.ToString();

        string GenerateBranches(ReadOnlySpan<TKey> span)
        {
            StringBuilder temp = new StringBuilder();

            for (int i = 0; i < span.Length; i++)
            {
                temp.AppendLine($$"""
                                          if ({{GetEqualFunction(LookupKeyName, ToValueLabel(span[i]))}})
                                          {
                                              value = _values[{{i.ToStringInvariant()}}];
                                              return true;
                                          }
                                  """);
            }

            return temp.ToString();
        }
    }

    private string GenerateSwitch(StringBuilder sb, ReadOnlySpan<TKey> data)
    {
        sb.Append($$"""
                        {{MethodAttribute}}
                        {{MethodModifier}}bool Contains({{KeyTypeName}} key)
                        {
                    {{GetMethodHeader(MethodType.Contains)}}

                            switch ({{LookupKeyName}})
                            {
                    {{FormatList(data, x => $"            case {ToValueLabel(x)}:", "\n")}}
                                    return true;
                                default:
                                    return false;
                            }
                        }
                    """);

        if (!ctx.Values.IsEmpty)
        {
            sb.Append($$"""
                            {{MethodAttribute}}
                            {{MethodModifier}}bool TryLookup({{KeyTypeName}} key, out {{ValueTypeName}}? value)
                            {
                                value = default;
                        {{GetMethodHeader(MethodType.TryLookup)}}
                                switch ({{LookupKeyName}})
                                {
                        {{GenerateSwitches(data)}}
                                }
                                value = default;
                                return false;
                            }
                        """);
        }

        return sb.ToString();

        string GenerateSwitches(ReadOnlySpan<TKey> span)
        {
            StringBuilder temp = new StringBuilder();

            for (int i = 0; i < span.Length; i++)
            {
                temp.AppendLine($"""
                                             case {ToValueLabel(span[i])}:
                                                 value = _values[{i.ToStringInvariant()}];
                                                 return true;
                                 """);
            }

            return temp.ToString();
        }
    }
}