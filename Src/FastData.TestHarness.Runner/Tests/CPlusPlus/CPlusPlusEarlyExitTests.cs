using Genbox.FastData.Generator.CPlusPlus.TestHarness;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.InternalShared.Harness.Enums;
using Genbox.FastData.TestHarness.Runner.Code.Abstracts;

namespace Genbox.FastData.TestHarness.Runner.Tests.CPlusPlus;

[Collection("Docker-CPlusPlus")]
public sealed class CPlusPlusEarlyExitTests(DockerCPlusPlusFixture fixture) : EarlyExitTestBase(new CPlusPlusTest(fixture.DockerManager), Bootstrap.CreateExpressionCompiler())
{
    private static readonly CPlusPlusBootstrap Bootstrap = new CPlusPlusBootstrap(HarnessType.Test);

    protected override string RenderProgram(string expression, object value) =>
        $$"""
          #include <cstdint>
          #include <string_view>

          static char ToLowerAscii(char value)
          {
              if (value >= 'A' && value <= 'Z')
                  return static_cast<char>(value + 32);
              return value;
          }

          static char GetCharAt(std::string_view str, int32_t offset) { return offset >= 0 ? str[offset] : str[str.length() + offset]; }
          static char GetCharAtLower(std::string_view str, int32_t offset) { return ToLowerAscii(offset >= 0 ? str[offset] : str[str.length() + offset]); }
          static int32_t GetLength(std::string_view str) { return static_cast<int32_t>(str.length()); }

          static bool StringAt(std::string_view fragment, int32_t offset, std::string_view str)
          {
              size_t start = offset >= 0 ? static_cast<size_t>(offset) : str.length() + offset;
              return str.compare(start, fragment.length(), fragment) == 0;
          }

          static bool StringAtIgnoreCase(std::string_view fragment, int32_t offset, std::string_view str)
          {
              size_t start = offset >= 0 ? static_cast<size_t>(offset) : str.length() + offset;
              for (size_t i = 0; i < fragment.length(); ++i)
              {
                  if (ToLowerAscii(str[start + i]) != ToLowerAscii(fragment[i]))
                      return false;
              }
              return true;
          }

          {{Bootstrap.Wrap($"""
                                    {Bootstrap.Map.GetTypeName(value.GetType())} inputKey = {Bootstrap.Map.ToValueLabel(value)};
                                    return {expression} ? 1 : 0;
                            """)}}
          """;
}