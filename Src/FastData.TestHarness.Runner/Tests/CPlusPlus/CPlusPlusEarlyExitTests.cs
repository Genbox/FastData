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

          static char GetCharAt(std::string_view str, int32_t index) { return str[index]; }
          static char GetCharAtLower(std::string_view str, int32_t index) { return ToLowerAscii(str[index]); }
          static char GetCharFromEnd(std::string_view str, int32_t fromEnd) { return str[str.length() - 1 - fromEnd]; }
          static char GetCharFromEndLower(std::string_view str, int32_t fromEnd) { return ToLowerAscii(str[str.length() - 1 - fromEnd]); }
          static int32_t GetLength(std::string_view str) { return static_cast<int32_t>(str.length()); }

          static bool StartsWith(std::string_view prefix, std::string_view str)
          {
              if (str.length() < prefix.length())
                  return false;
              return str.compare(0, prefix.length(), prefix) == 0;
          }

          static bool StartsWithIgnoreCase(std::string_view prefix, std::string_view str)
          {
              if (str.length() < prefix.length())
                  return false;

              for (std::size_t i = 0; i < prefix.length(); ++i)
              {
                  if (ToLowerAscii(str[i]) != ToLowerAscii(prefix[i]))
                      return false;
              }

              return true;
          }

          static bool EndsWith(std::string_view suffix, std::string_view str)
          {
              if (str.length() < suffix.length())
                  return false;
              return str.compare(str.length() - suffix.length(), suffix.length(), suffix) == 0;
          }

          static bool EndsWithIgnoreCase(std::string_view suffix, std::string_view str)
          {
              if (str.length() < suffix.length())
                  return false;

              std::size_t offset = str.length() - suffix.length();
              for (std::size_t i = 0; i < suffix.length(); ++i)
              {
                  if (ToLowerAscii(str[offset + i]) != ToLowerAscii(suffix[i]))
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