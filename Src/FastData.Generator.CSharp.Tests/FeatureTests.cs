using Genbox.FastData.Enums;
using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.InternalShared;
using Genbox.FastData.InternalShared.Helpers;
using Genbox.FastData.InternalShared.TestClasses;
using Genbox.FastData.InternalShared.TestClasses.TheoryData;
using static Genbox.FastData.Generator.Helpers.FormatHelper;
using static Genbox.FastData.InternalShared.Helpers.TestHelper;

namespace Genbox.FastData.Generator.CSharp.Tests;

public class FeatureTests
{
    [Theory]
    [ClassData(typeof(SimpleTestVectorTheoryData))]
    public async Task ObjectSupportTest<T>(TestVector<T> vector)
    {
        Person[] values =
        [
            new Person { Age = 1, Name = "Bob", Other = new Person { Name = "Anna", Age = 4 } },
            new Person { Age = 2, Name = "Billy" },
            new Person { Age = 3, Name = "Bibi" },
        ];

        GeneratorSpec spec = Generate(id => CSharpCodeGenerator.Create(new CSharpCodeGeneratorConfig(id)), vector, values);

        string id = $"{nameof(ObjectSupportTest)}-{spec.Identifier}";

        await Verify(spec.Source)
              .UseFileName(id)
              .UseDirectory("Features")
              .DisableDiff();

        CSharpLanguageDef langDef = new CSharpLanguageDef();
        TypeMap map = new TypeMap(langDef.TypeDefinitions, GeneratorEncoding.ASCII);

        string source = $$"""
                          {{spec.Source}}

                          public static class Wrapper
                          {
                              public static bool Run()
                              {
                          {{FormatList(vector.Keys, x => $"""
                                                              if (!{spec.Identifier}.TryLookup({map.ToValueLabel(x)}, out _))
                                                                  return false;
                                                          """, "\n")}}

                                  return true;
                              }
                          }
                          """;

        Func<bool> contains = CompilationHelper.GetDelegate<Func<bool>>(source, types => types.First(x => x.Name == "Wrapper"), false);
        Assert.True(contains());
    }

    [Theory]
    [ClassData(typeof(FloatNaNZeroTestVectorTheoryData))]
    public async Task FloatNaNOrZeroHashSupportTest<T>(TestVector<T> vector)
    {
        GeneratorSpec spec = Generate(id => CSharpCodeGenerator.Create(new CSharpCodeGeneratorConfig(id)), vector);

        string id = $"{nameof(FloatNaNOrZeroHashSupportTest)}-{spec.Identifier}";

        await Verify(spec.Source)
              .UseFileName(id)
              .UseDirectory("Features")
              .DisableDiff();

        CSharpLanguageDef langDef = new CSharpLanguageDef();
        TypeMap map = new TypeMap(langDef.TypeDefinitions, GeneratorEncoding.ASCII);

        string source = $$"""
                          {{spec.Source}}

                          public static class Wrapper
                          {
                              public static bool Run()
                              {
                          {{FormatList(vector.Keys, x => $"""
                                                              if (!{spec.Identifier}.Contains({map.ToValueLabel(x)}))
                                                                  return false;
                                                          """, "\n")}}

                                  return true;
                              }
                          }
                          """;

        Func<bool> contains = CompilationHelper.GetDelegate<Func<bool>>(source, types => types.First(x => x.Name == "Wrapper"), false);
        Assert.True(contains());
    }

    internal class Person
    {
        public int Age { get; set; }
        public string Name { get; set; }
        public Person Other { get; set; }
    }
}