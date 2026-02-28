using System.Globalization;
using System.Text;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Helpers;
using Genbox.FastData.Generators;
using Microsoft.VisualStudio.TextTemplating;
using Mono.TextTemplating;

namespace Genbox.FastData.Generator.Template.Helpers;

public static class TemplateHelper
{
    public static string Render<TKey>(OutputWriter<TKey> writer, string name, string source, Dictionary<string, object?> variables)
    {
        TemplateGenerator generator = new TemplateGenerator();
        AddTemplateReference(generator, typeof(CommonDataModel));
        AddTemplateReference(generator, typeof(TypeCode));
        AddTemplateReference(generator, typeof(FormatHelper));

        ITextTemplatingSession session = generator.GetOrCreateSession();

        session["Common"] = new CommonDataModel
        {
            InputKeyName = writer.InputKeyName,
            LookupKeyName = writer.LookupKeyName,
            ArraySizeType = writer.ArraySizeType,
            HashSizeType = writer.HashSizeType
        };

        foreach (KeyValuePair<string, object?> pair in variables)
        {
            if (pair.Value == null)
                continue;

            session.Add(pair.Key, pair.Value);
            AddTemplateReference(generator, pair.Value.GetType());
        }

        ParsedTemplate parsed = generator.ParseTemplate(name, source);

        TemplateSettings settings = TemplatingEngine.GetSettings(generator, parsed);
        settings.Culture = CultureInfo.InvariantCulture;
        settings.Debug = false;
        settings.Encoding = Encoding.UTF8;

        ValueTuple<string, string> result = generator.ProcessTemplateAsync(parsed, name, source, name, settings).GetAwaiter().GetResult();

        if (generator.Errors.HasErrors)
        {
            string errors = string.Join("\n", generator.Errors.Cast<object>().Select(x => x.ToString()));
            throw new InvalidOperationException("Failed to process template '" + name + "':\n" + errors);
        }

        return result.Item2;
    }

    private static void AddTemplateReference(TemplateGenerator generator, Type type)
    {
        string location = type.Assembly.Location;

        if (!generator.Refs.Exists(x => string.Equals(x, location, StringComparison.OrdinalIgnoreCase)))
            generator.Refs.Add(location);
    }
}