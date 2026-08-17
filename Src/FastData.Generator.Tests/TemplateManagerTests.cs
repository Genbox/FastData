using System.Globalization;
using Genbox.FastData.Generator.Template.Helpers;

namespace Genbox.FastData.Generator.Tests;

public class TemplateManagerTests
{
    private const string _template = "<#@ parameter type=\"System.String\" name=\"Value\" #><#= Value #>";

    [Fact]
    public void ReusesTemplateWithDifferentVariables()
    {
        TemplateManager manager = new TemplateManager("ReuseTests", false);

        string first = manager.Render("Echo.tt", _template, CreateVariables("first"));
        string second = manager.Render("Echo.tt", _template, CreateVariables("second"));

        Assert.Equal("first", first);
        Assert.Equal("second", second);
    }

    [Fact]
    public void DistinguishesDifferentSourcesWithSameFileName()
    {
        TemplateManager manager = new TemplateManager("SourceTests", false);

        string first = manager.Render("Echo.tt", "first <#= 1 #>", []);
        string second = manager.Render("Echo.tt", "second <#= 2 #>", []);

        Assert.Equal("first 1", first);
        Assert.Equal("second 2", second);
    }

    [Fact]
    public void DistinguishesDifferentReferenceSets()
    {
        TemplateManager manager = new TemplateManager("ReferenceTests", false);
        const string typeName = "Genbox.FastData.TemplateCache.ReferenceTests.ReferenceEcho";

        manager.Render("ReferenceEcho.tt", "output", []);
        manager.Render("ReferenceEcho.tt", "output", new Dictionary<string, object?> { { "Unused", new ReferenceMarker() } });

        int loadedTemplates = AppDomain.CurrentDomain.GetAssemblies().Count(assembly => assembly.GetType(typeName, false) != null);
        Assert.Equal(2, loadedTemplates);
    }

    [Fact]
    public async Task RendersTemplateConcurrently()
    {
        TemplateManager manager = new TemplateManager("ConcurrencyTests", false);
        Task<string>[] tasks = Enumerable.Range(0, 8)
                                         .Select(index => Task.Run(() => manager.Render("Echo.tt", _template, CreateVariables(index.ToString(CultureInfo.InvariantCulture)))))
                                         .ToArray();

        string[] results = await Task.WhenAll(tasks);

        Assert.Equal(Enumerable.Range(0, 8).Select(index => index.ToString(CultureInfo.InvariantCulture)), results);
    }

    private static Dictionary<string, object?> CreateVariables(string value) => new Dictionary<string, object?> { { "Value", value } };

    private sealed class ReferenceMarker {}
}